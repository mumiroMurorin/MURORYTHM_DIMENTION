using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Mediapipe.Unity.CoordinateSystem;
using static WebCamUtils;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using Stopwatch = System.Diagnostics.Stopwatch; // for Timestamp

namespace Mediapipe.Unity.Tutorial
{
    public class BodyTracking : MonoBehaviour, ICameraInfoHolder
    {
        [SerializeField] private RawImage _screen;

        // Pose tracking settings
        [SerializeField] private BodyTrackingModelComplexity _BodyTrackingModelComplexity = BodyTrackingModelComplexity.Full;
        [SerializeField] private TextAsset _configAsset;

        [Header("Width")]
        [SerializeField] private int _width;
        [Header("Height")]
        [SerializeField] private int _height;
        [Header("FPS")]
        [SerializeField] private int _fps;

        private ReactiveProperty<int> fps = new ReactiveProperty<int>();
        public IReadOnlyReactiveProperty<int> CameraFps => fps;

        private BodyTrackingSettings settings;
        private CalculatorGraph _graph;
        // Camera texture
        private ReactiveProperty<WebCamTexture> _webCamTexture = new ReactiveProperty<WebCamTexture>();
        public IReadOnlyReactiveProperty<WebCamTexture> WebCamInfo => _webCamTexture;

        private Texture2D _inputTexture;
        private Color32[] _pixelData;

        private bool isReadyTracking;

        private NormalizedLandmarkList landmarkList;
        public NormalizedLandmarkList LandmarkList { get { return landmarkList; } }
        private OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList> poseLandmarksStream;

        private CancellationTokenSource initializeCts = new CancellationTokenSource();
        private CancellationTokenSource trackingCts = new CancellationTokenSource();

        /// <summary>
        /// Initialize pose tracking.
        /// </summary>
        public void Initialize(BodyTrackingSettings settings = default)
        {
            this.settings = settings;

            initializeCts.CancelAndDispose();
            initializeCts = new CancellationTokenSource();

            InitializeAsync(this.settings, initializeCts.Token).Forget();
        }

        /// <summary>
        /// Start tracking.
        /// </summary>
        [Obsolete]
        public void StartTracking()
        {
            trackingCts.CancelAndDispose();
            trackingCts = new CancellationTokenSource();

            BodyTrackAsync(trackingCts.Token).Forget();
        }

        private async UniTask InitializeAsync(BodyTrackingSettings settings, CancellationToken token)
        {
            isReadyTracking = false;

            // Check webcam availability
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogError("[MediaPipe] No webcam devices found!");
                return;
            }

            if (WebCamTexture.devices.Length <= settings.CameraIndex) { settings.CameraIndex = 0; }
            var webCamDevice = WebCamTexture.devices[settings.CameraIndex];

            // Check supported resolution
            var isSurpported = await CheckIfTextureStartedAsync(webCamDevice.name, settings.CameraWidth.Value, settings.CameraHeight.Value, token);
            if (isSurpported)
            {
                _webCamTexture.Value = new WebCamTexture(webCamDevice.name, settings.CameraWidth.Value, settings.CameraHeight.Value);
            }
            else
            {
                _webCamTexture.Value = new WebCamTexture(webCamDevice.name, _width, _height);
            }

            _webCamTexture.Value.Play();
            Debug.Log("[MediaPipe] WebCamTexture is playing: " + _webCamTexture.Value.isPlaying);

            try
            {
                await UniTask.WaitUntil(() => _webCamTexture.Value.width > 16, cancellationToken: token);

                _width = _webCamTexture.Value.width;
                _height = _webCamTexture.Value.height;
                Debug.Log($"[MediaPipe] WebCamTexture is set resolution: {_width}x{_height}");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("[MediaPipe] Initialization canceled during webcam wait.");
                return;
            }

            if (_webCamTexture.Value.width <= 16)
            {
                Debug.LogError("[MediaPipe] WebCamTexture did not initialize correctly.");
                return;
            }

            Debug.Log("[MediaPipe] WebCamTexture initialized successfully.");

            _screen.rectTransform.sizeDelta = new Vector2(_width, _height);
            _screen.texture = _webCamTexture.Value;

            _inputTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
            _pixelData = new Color32[_webCamTexture.Value.width * _webCamTexture.Value.height];

            // Load model assets
            switch (_BodyTrackingModelComplexity)
            {
                case BodyTrackingModelComplexity.Lite:
                    Debug.Log("[MediaPipe] Loading Lite pose model...");
                    await LoadModelAssets("pose_landmark_lite.bytes");
                    await LoadModelAssets("pose_detection.bytes");
                    break;
                case BodyTrackingModelComplexity.Full:
                    Debug.Log("[MediaPipe] Loading Full pose model...");
                    await LoadModelAssets("pose_landmark_full.bytes");
                    await LoadModelAssets("pose_detection.bytes");
                    break;
                case BodyTrackingModelComplexity.Heavy:
                    Debug.Log("[MediaPipe] Loading Heavy pose model...");
                    await LoadModelAssets("pose_landmark_heavy.bytes");
                    await LoadModelAssets("pose_detection.bytes");
                    break;
            }

            if (_configAsset == null)
            {
                Debug.LogError("[MediaPipe] Configuration asset (_configAsset) is not assigned.");
                return;
            }

            _graph = new CalculatorGraph(_configAsset.text);
            if (_graph == null)
            {
                Debug.LogError("[MediaPipe] Failed to initialize CalculatorGraph.");
                return;
            }

            poseLandmarksStream = new OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList>(_graph, "pose_landmarks");
            poseLandmarksStream.StartPolling().AssertOk();

            var sidePacket = new SidePacket();
            sidePacket.Emplace("model_complexity", new IntPacket((int)_BodyTrackingModelComplexity));
            sidePacket.Emplace("input_rotation", new IntPacket(0));
            sidePacket.Emplace("input_horizontally_flipped", new BoolPacket(settings.IsHorizontallyFlipped.Value));
            sidePacket.Emplace("input_vertically_flipped", new BoolPacket(settings.IsVerticallyFlipped.Value));
            sidePacket.Emplace("smooth_landmarks", new BoolPacket(true));
            sidePacket.Emplace("enable_segmentation", new BoolPacket(true));
            sidePacket.Emplace("smooth_segmentation", new BoolPacket(true));
            sidePacket.Emplace("output_rotation", new IntPacket(0));
            sidePacket.Emplace("output_horizontally_flipped", new BoolPacket(false));
            sidePacket.Emplace("output_vertically_flipped", new BoolPacket(false));

            _graph.StartRun(sidePacket).AssertOk();

            isReadyTracking = true;
            Debug.Log("[MediaPipe] Graph started successfully!");
        }

        [Obsolete]
        private async UniTask BodyTrackAsync(CancellationToken token)
        {
            await UniTask.WaitUntil(() => isReadyTracking == true, cancellationToken: token);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            while (true)
            {
                _inputTexture.SetPixels32(_webCamTexture.Value.GetPixels32(_pixelData));
                using var imageFrame = new ImageFrame(ImageFormat.Types.Format.Srgba, _width, _height, _width * 4, _inputTexture.GetRawTextureData<byte>());
                var currentTimestamp = stopwatch.ElapsedTicks / (System.TimeSpan.TicksPerMillisecond / 1000);
                using var imageFramePacket = new ImageFramePacket(imageFrame, new Timestamp(currentTimestamp));

                _graph.AddPacketToInputStream("input_video", imageFramePacket).AssertOk();
                float start = Time.realtimeSinceStartup;

                await UniTask.WaitForEndOfFrame(token);

                float end = Time.realtimeSinceStartup;
                float deltaTime = end - start;
                _fps = deltaTime > 0 ? (int)(1f / deltaTime) : 0;
                fps.Value = _fps;

                if (poseLandmarksStream.TryGetNext(out var LandMarks))
                {
                    if (LandMarks == null) { continue; }

                    landmarkList = LandMarks;
                }
            }
        }

        // Load a model file.
        private IEnumerator LoadModelAssets(string assetName)
        {
            yield return MediaPipeResourceManagerProvider.Instance.PrepareAssetAsync(assetName);
            Debug.Log($"[MediaPipe] Loaded {assetName}");
        }

        private void OnDestroy()
        {
            if (_webCamTexture.Value != null)
            {
                _webCamTexture.Value.Stop();
            }

            if (_graph != null)
            {
                try
                {
                    _graph.CloseInputStream("input_video").AssertOk();
                    _graph.WaitUntilDone().AssertOk();
                }
                finally
                {
                    _graph.Dispose();
                    Debug.Log("[MediaPipe] Graph disposed.");
                }
            }

            initializeCts.CancelAndDispose();
            trackingCts.CancelAndDispose();
        }
    }
}

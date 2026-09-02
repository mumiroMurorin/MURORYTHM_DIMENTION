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
    public class HandTracking : MonoBehaviour, ICameraInfoHolder
    {
        private const int HAND_NUM = 2;

        [SerializeField] private RawImage _screen;

        // Hand tracking settings
        [SerializeField] private HandTrackingModelComplexity _HandTrackingModelComplexity = HandTrackingModelComplexity.Lite;
        [SerializeField] private TextAsset _configAsset;

        [Header("Width")]
        [SerializeField] private int _width;
        [Header("Height")]
        [SerializeField] private int _height;
        [Header("FPS")]
        [SerializeField] private int _fps;
        [Header("Process Frame Interval")]
        [SerializeField, Min(1)] private int _processFrameInterval = 1;

        [SerializeField, Min(1)] private int _maxResultsToDrainPerFrame = 8;

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

        private List<NormalizedLandmarkList> landmarkList;
        public List<NormalizedLandmarkList> LandmarkList { get { return landmarkList; } }
        private OutputStream<NormalizedLandmarkListVectorPacket, List<NormalizedLandmarkList>> handLandmarksStream;

        private CancellationTokenSource initializeCts = new CancellationTokenSource();
        private CancellationTokenSource trackingCts = new CancellationTokenSource();

        /// <summary>
        /// Initialize hand tracking.
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

            switch (_HandTrackingModelComplexity)
            {
                case HandTrackingModelComplexity.Lite:
                    Debug.Log("[MediaPipe] Loading Lite hand model...");
                    await LoadModelAssets("hand_landmark_lite.bytes");
                    await LoadModelAssets("hand_recrop.bytes");
                    await LoadModelAssets("handedness.txt");
                    await LoadModelAssets("palm_detection_lite.bytes");
                    break;
                case HandTrackingModelComplexity.Full:
                    Debug.Log("[MediaPipe] Loading Full hand model...");
                    await LoadModelAssets("hand_landmark_full.bytes");
                    await LoadModelAssets("hand_recrop.bytes");
                    await LoadModelAssets("handedness.txt");
                    await LoadModelAssets("palm_detection_full.bytes");
                    break;
            }

            if (_configAsset == null)
            {
                Debug.LogError("[MediaPipe] Configuration asset (_configAsset) is not assigned.");
                return;
            }

            var graphConfig = CalculatorGraphConfig.Parser.ParseFromTextFormat(_configAsset.text);
            var handLandmarksPresenceStream = graphConfig.AddPacketPresenceCalculator("hand_landmarks");
            _graph = new CalculatorGraph(graphConfig);
            if (_graph == null)
            {
                Debug.LogError("[MediaPipe] Failed to initialize CalculatorGraph.");
                return;
            }

            handLandmarksStream = new OutputStream<NormalizedLandmarkListVectorPacket, List<NormalizedLandmarkList>>(
                _graph,
                "hand_landmarks",
                handLandmarksPresenceStream);
            handLandmarksStream.StartPolling().AssertOk();

            var sidePacket = new SidePacket();
            sidePacket.Emplace("model_complexity", new IntPacket((int)_HandTrackingModelComplexity));
            sidePacket.Emplace("num_hands", new IntPacket(HAND_NUM));
            sidePacket.Emplace("input_rotation", new IntPacket(0));
            sidePacket.Emplace("input_horizontally_flipped", new BoolPacket(settings.IsHorizontallyFlipped.Value));
            sidePacket.Emplace("input_vertically_flipped", new BoolPacket(settings.IsVerticallyFlipped.Value));

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

            var lastFpsUpdateTime = Time.realtimeSinceStartup;
            var processedFrameCount = 0;

            while (true)
            {
                if (_processFrameInterval > 1 && Time.frameCount % _processFrameInterval != 0)
                {
                    await UniTask.WaitForEndOfFrame(token);
                    continue;
                }

                if (!_webCamTexture.Value.didUpdateThisFrame)
                {
                    DrainLatestLandmarks();
                    await UniTask.WaitForEndOfFrame(token);
                    continue;
                }

                _inputTexture.SetPixels32(_webCamTexture.Value.GetPixels32(_pixelData));
                using var imageFrame = new ImageFrame(ImageFormat.Types.Format.Srgba, _width, _height, _width * 4, _inputTexture.GetRawTextureData<byte>());
                var currentTimestamp = stopwatch.ElapsedTicks / (System.TimeSpan.TicksPerMillisecond / 1000);
                using var imageFramePacket = new ImageFramePacket(imageFrame, new Timestamp(currentTimestamp));

                _graph.AddPacketToInputStream("input_video", imageFramePacket).AssertOk();
                processedFrameCount++;
                DrainLatestLandmarks();

                var now = Time.realtimeSinceStartup;
                var elapsed = now - lastFpsUpdateTime;
                if (elapsed >= 1f)
                {
                    _fps = Mathf.RoundToInt(processedFrameCount / elapsed);
                    if (fps.Value != _fps) { fps.Value = _fps; }
                    processedFrameCount = 0;
                    lastFpsUpdateTime = now;
                }

                await UniTask.WaitForEndOfFrame(token);
            }
        }

        private void DrainLatestLandmarks()
        {
            List<NormalizedLandmarkList> latestLandmarks = null;

            for (var i = 0; i < _maxResultsToDrainPerFrame; i++)
            {
                if (!handLandmarksStream.TryGetNext(out var nextLandmarks, false)) { break; }
                if (nextLandmarks != null) { latestLandmarks = nextLandmarks; }
            }

            if (latestLandmarks != null)
            {
                landmarkList = latestLandmarks;
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

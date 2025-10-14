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
        const int HAND_NUM = 2;

        [SerializeField] private RawImage _screen;

        // --- Handトラッキング用 ---

        private enum ModelComplexity { Lite = 0, Full = 1, }
        [SerializeField] ModelComplexity _modelComplexity = ModelComplexity.Lite;
        [SerializeField] private TextAsset _configAsset;

        [Header("横幅(確認用)")]
        [SerializeField] private int _width;
        [Header("縦幅(確認用)")]
        [SerializeField] private int _height;
        [Header("FPS(確認用)")]
        [SerializeField] private int _fps;

        ReactiveProperty<int> fps = new ReactiveProperty<int>();
        public IReadOnlyReactiveProperty<int> CameraFps => fps;

        BodyTrackingSettings settings;
        CalculatorGraph _graph;
        static ResourceManager _resourceManager;

        // カメラ入力用
        ReactiveProperty<WebCamTexture> _webCamTexture = new ReactiveProperty<WebCamTexture>();
        public IReadOnlyReactiveProperty<WebCamTexture> WebCamInfo => _webCamTexture;

        Texture2D _inputTexture;
        Color32[] _pixelData;

        bool isReadyTracking;

        List<NormalizedLandmarkList> landmarkList;
        public List<NormalizedLandmarkList> LandmarkList { get { return landmarkList; } }
        OutputStream<NormalizedLandmarkListVectorPacket, List<NormalizedLandmarkList>> handLandmarksStream;

        CancellationTokenSource initializeCts = new CancellationTokenSource();
        CancellationTokenSource trackingCts = new CancellationTokenSource();

        /// <summary>
        /// 非同期初期化関数のラップ
        /// </summary>
        public void Initialize(BodyTrackingSettings settings = default)
        {
            this.settings = settings;

            initializeCts.CancelAndDispose();
            initializeCts = new CancellationTokenSource();

            InitializeAsync(this.settings, initializeCts.Token).Forget();
        }

        /// <summary>
        /// 非同期トラッキング関数のラップ
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

            // Webカメラの初期化チェック
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogError("【MediaPipe】No webcam devices found!");
                return;
            }

            if (WebCamTexture.devices.Length <= settings.CameraIndex) { settings.CameraIndex = 0; } 
            var webCamDevice = WebCamTexture.devices[settings.CameraIndex];

            // 解像度対応チェック
            var isSurpported = await CheckIfTextureStartedAsync(webCamDevice.name, settings.CameraWidth.Value, settings.CameraHeight.Value, token);
            // 対応してたらその解像度にする
            if (isSurpported) 
            { 
                _webCamTexture.Value = new WebCamTexture(webCamDevice.name, settings.CameraWidth.Value, settings.CameraHeight.Value);
            }
            // 対応してなかったらデフォルト(最高?)解像度にする
            else 
            {
                _webCamTexture.Value = new WebCamTexture(webCamDevice.name, _width, _height);
            }

            _webCamTexture.Value.Play();
            Debug.Log("【MediaPipe】WebCamTexture is playing: " + _webCamTexture.Value.isPlaying);

            try
            {
                await UniTask.WaitUntil(() => _webCamTexture.Value.width > 16, cancellationToken: token);

                // 解像度の動的設定
                _width = _webCamTexture.Value.width;
                _height = _webCamTexture.Value.height;
                Debug.Log($"【MediaPipe】WebCamTexture is set resolution: {_width}x{_height}");
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("【MediaPipe】Initialization canceled during webcam wait.");
                return;
            }

            if (_webCamTexture.Value.width <= 16)
            {
                Debug.LogError("【MediaPipe】WebCamTexture did not initialize correctly.");
                return;
            }

            Debug.Log("【MediaPipe】WebCamTexture initialized successfully.");

            _screen.rectTransform.sizeDelta = new Vector2(_width, _height);
            _screen.texture = _webCamTexture.Value;

            _inputTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
            _pixelData = new Color32[_webCamTexture.Value.width * _webCamTexture.Value.height];

            _resourceManager ??= new StreamingAssetsResourceManager();

            // LoadModelAssetsにCancel処理が組み込めなかったから凄く不安…
            switch (_modelComplexity)
            {
                case ModelComplexity.Lite:
                    Debug.Log("【MediaPipe】Loading Lite Pose model...");
                    await LoadModelAssets("hand_landmark_lite.bytes");
                    await LoadModelAssets("hand_recrop.bytes");
                    await LoadModelAssets("handedness.txt");
                    await LoadModelAssets("palm_detection_lite.bytes");
                    break;
                case ModelComplexity.Full:
                    Debug.Log("【MediaPipe】Loading Full Pose model...");
                    await LoadModelAssets("hand_landmark_full.bytes");
                    await LoadModelAssets("hand_recrop.bytes");
                    await LoadModelAssets("handedness.txt");
                    await LoadModelAssets("palm_detection_full.bytes");
                    break;
            }

            if (_configAsset == null)
            {
                Debug.LogError("【MediaPipe】Configuration asset (_configAsset) is not assigned.");
                return;
            }

            _graph = new CalculatorGraph(_configAsset.text);
            if (_graph == null)
            {
                Debug.LogError("【MediaPipe】Failed to initialize CalculatorGraph.");
                return;
            }

            handLandmarksStream = new OutputStream<NormalizedLandmarkListVectorPacket, List<NormalizedLandmarkList>>(_graph, "hand_landmarks");
            handLandmarksStream.StartPolling().AssertOk();

            var sidePacket = new SidePacket();
            sidePacket.Emplace("model_complexity", new IntPacket((int)_modelComplexity));
            sidePacket.Emplace("num_hands", new IntPacket(HAND_NUM));
            sidePacket.Emplace("input_rotation", new IntPacket(0));
            sidePacket.Emplace("input_horizontally_flipped", new BoolPacket(settings.IsHorizontallyFlipped.Value));
            sidePacket.Emplace("input_vertically_flipped", new BoolPacket(settings.IsVerticallyFlipped.Value));

            _graph.StartRun(sidePacket).AssertOk();

            isReadyTracking = true;
            Debug.Log("【MediaPipe】Graph started successfully!");
        }

        [Obsolete]
        private async UniTask BodyTrackAsync(CancellationToken token)
        {
            await UniTask.WaitUntil(() => isReadyTracking == true, cancellationToken: token);
            // タイマー開始
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // var screenRect = _screen.GetComponent<RectTransform>().rect;

            while (true)
            {
                _inputTexture.SetPixels32(_webCamTexture.Value.GetPixels32(_pixelData));
                var imageFrame = new ImageFrame(ImageFormat.Types.Format.Srgba, _width, _height, _width * 4, _inputTexture.GetRawTextureData<byte>());
                var currentTimestamp = stopwatch.ElapsedTicks / (System.TimeSpan.TicksPerMillisecond / 1000);

                _graph.AddPacketToInputStream("input_video", new ImageFramePacket(imageFrame, new Timestamp(currentTimestamp))).AssertOk();
                float start = Time.realtimeSinceStartup;

                await UniTask.WaitForEndOfFrame(token);

                // FPSの更新
                float end = Time.realtimeSinceStartup;
                float deltaTime = end - start;
                _fps = (int)(1f / deltaTime);
                fps.Value = _fps;

                if (handLandmarksStream.TryGetNext(out var LandMarks))
                {
                    if (LandMarks == null) { continue; }

                    landmarkList = LandMarks;
                }
                else
                {
                    // Debug.LogWarning("【MediaPipe】No pose landmarks received.");
                }
            }
        }

        // モデル読み込み補助メソッド
        private IEnumerator LoadModelAssets(string assetName)
        {
            yield return _resourceManager.PrepareAssetAsync(assetName);
            Debug.Log($"【MediaPipe】Loaded {assetName}");
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
                    Debug.Log("【MediaPipe】Graph disposed.");
                }
            }

            initializeCts.CancelAndDispose();
            trackingCts.CancelAndDispose();
        }
    }
}

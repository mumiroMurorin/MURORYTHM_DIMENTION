using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using Mediapipe.Unity.CoordinateSystem;
using System.Threading;
using Cysharp.Threading.Tasks;
using Stopwatch = System.Diagnostics.Stopwatch; // for Timestamp

namespace Mediapipe.Unity.Tutorial
{
    public class BodyTracking : MonoBehaviour
    {
        [SerializeField] private RawImage _screen;

        // --- Pose(ボディ)トラッキング用 ---

        private enum ModelComplexity { Lite = 0, Full = 1,Heavy = 2 }
        [SerializeField] ModelComplexity _modelComplexity = ModelComplexity.Full;
        [SerializeField] private TextAsset _configAsset;

        [Header("横幅(確認用)")]
        [SerializeField] private int _width;
        [Header("縦幅(確認用)")]
        [SerializeField] private int _height;
        [Header("FPS(確認用)")]
        [SerializeField] private int _fps;
        
        BodyTrackingSettings settings;
        CalculatorGraph _graph;
        static ResourceManager _resourceManager;

        // カメラ入力用
        WebCamTexture _webCamTexture;
        Texture2D _inputTexture;
        Color32[] _pixelData;

        NormalizedLandmarkList landmarkList;
        public NormalizedLandmarkList LandmarkList { get { return landmarkList; } }
        OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList> poseLandmarksStream;

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
            // Webカメラの初期化チェック
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogError("【MediaPipe】No webcam devices found!");
                return;
            }

            var webCamDevice = WebCamTexture.devices[0];
            _webCamTexture = new WebCamTexture(webCamDevice.name, _width, _height);
            _webCamTexture.Play();
            Debug.Log("【MediaPipe】WebCamTexture is playing: " + _webCamTexture.isPlaying);

            try
            {
                await UniTask.WaitUntil(() => _webCamTexture.width > 16, cancellationToken: token);

                // 解像度の動的設定
                _width = _webCamTexture.width;
                _height = _webCamTexture.height;
            }
            catch (OperationCanceledException)
            {
                Debug.LogWarning("【MediaPipe】Initialization canceled during webcam wait.");
                return;
            }

            if (_webCamTexture.width <= 16)
            {
                Debug.LogError("【MediaPipe】WebCamTexture did not initialize correctly.");
                return;
            }

            Debug.Log("【MediaPipe】WebCamTexture initialized successfully.");

            _screen.rectTransform.sizeDelta = new Vector2(_width, _height);
            _screen.texture = _webCamTexture;

            _inputTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
            _pixelData = new Color32[_webCamTexture.width * _webCamTexture.height];

            _resourceManager ??= new StreamingAssetsResourceManager();

            // LoadModelAssetsにCancel処理が組み込めなかったから凄く不安…
            switch (_modelComplexity)
            {
                case ModelComplexity.Lite:
                    Debug.Log("【MediaPipe】Loading Lite Pose model...");
                    await LoadModelAssets("pose_landmark_lite.bytes");
                    await LoadModelAssets("pose_detection.bytes");
                    break;
                case ModelComplexity.Full:
                    Debug.Log("【MediaPipe】Loading Full Pose model...");
                    await LoadModelAssets("pose_landmark_full.bytes");
                    await LoadModelAssets("pose_detection.bytes");
                    break;
                case ModelComplexity.Heavy:
                    Debug.Log("【MediaPipe】Loading Heavy Pose model...");
                    await LoadModelAssets("pose_landmark_heavy.bytes");
                    await LoadModelAssets("pose_detection.bytes");
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

            poseLandmarksStream = new OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList>(_graph, "pose_landmarks");
            poseLandmarksStream.StartPolling().AssertOk();

            var sidePacket = new SidePacket();
            sidePacket.Emplace("model_complexity", new IntPacket((int)_modelComplexity));
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
            Debug.Log("【MediaPipe】Graph started successfully!");
        }

        [Obsolete]
        private async UniTask BodyTrackAsync(CancellationToken token)
        {
            // タイマー開始
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // var screenRect = _screen.GetComponent<RectTransform>().rect;

            while (true)
            {
                _inputTexture.SetPixels32(_webCamTexture.GetPixels32(_pixelData));
                var imageFrame = new ImageFrame(ImageFormat.Types.Format.Srgba, _width, _height, _width * 4, _inputTexture.GetRawTextureData<byte>());
                var currentTimestamp = stopwatch.ElapsedTicks / (System.TimeSpan.TicksPerMillisecond / 1000);

                _graph.AddPacketToInputStream("input_video", new ImageFramePacket(imageFrame, new Timestamp(currentTimestamp))).AssertOk();
                float start = Time.realtimeSinceStartup;

                await UniTask.WaitForEndOfFrame(token);

                float end = Time.realtimeSinceStartup;
                float deltaTime = end - start;
                _fps = (int)(1f / deltaTime);

                if (poseLandmarksStream.TryGetNext(out var LandMarks))
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
            if (_webCamTexture != null)
            {
                _webCamTexture.Stop();
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

        // もともと書いてあったやつ
        //private IEnumerator Start()
        //{
        //    // Webカメラの初期化チェック
        //    if (WebCamTexture.devices.Length == 0)
        //    {
        //        Debug.LogError("【MediaPipe】No webcam devices found!");
        //        yield break;
        //    }

        //    var webCamDevice = WebCamTexture.devices[0];
        //    _webCamTexture = new WebCamTexture(webCamDevice.name, _width, _height, _fps);
        //    _webCamTexture.Play();
        //    Debug.Log("【MediaPipe】WebCamTexture is playing: " + _webCamTexture.isPlaying);

        //    // Webカメラの解像度が16以上になるまで待機
        //    yield return new WaitUntil(() => _webCamTexture.width > 16);
        //    if (_webCamTexture.width <= 16)
        //    {
        //        Debug.LogError("【MediaPipe】WebCamTexture did not initialize correctly.");
        //        yield break;
        //    }
        //    else
        //    {
        //        Debug.Log("【MediaPipe】WebCamTexture initialized successfully.");
        //    }

        //    // UI のセットアップ
        //    _screen.rectTransform.sizeDelta = new Vector2(_width, _height);
        //    _screen.texture = _webCamTexture;

        //    // _inputTexture と _pixelData の初期化
        //    _inputTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
        //    _pixelData = new Color32[_webCamTexture.width * _webCamTexture.height];

        //    // リソースマネージャーの初期化
        //    if (_resourceManager == null)
        //    {
        //        _resourceManager = new StreamingAssetsResourceManager();
        //    }

        //    // モデルの読み込み（ポーズトラッキング用に変更）
        //    if (_modelComplexity == ModelComplexity.Lite)
        //    {
        //        Debug.Log("【MediaPipe】Loading Lite Pose model...");
        //        yield return LoadModelAssets("pose_landmark_lite.bytes");
        //        yield return LoadModelAssets("pose_detection.bytes");
        //    }
        //    else if (_modelComplexity == ModelComplexity.Full)
        //    {
        //        Debug.Log("【MediaPipe】Loading Full Pose model...");
        //        yield return LoadModelAssets("pose_landmark_full.bytes");
        //        yield return LoadModelAssets("pose_detection.bytes");
        //    }
        //    else if (_modelComplexity == ModelComplexity.Heavy)
        //    {
        //        Debug.Log("【MediaPipe】Loading Full Pose model...");
        //        yield return LoadModelAssets("pose_landmark_heavy.bytes");
        //        yield return LoadModelAssets("pose_detection.bytes");
        //    }

        //    // コンフィグアセットのチェック
        //    if (_configAsset == null)
        //    {
        //        Debug.LogError("【MediaPipe】Configuration asset (_configAsset) is not assigned.");
        //        yield break;
        //    }
        //    _graph = new CalculatorGraph(_configAsset.text);
        //    if (_graph == null)
        //    {
        //        Debug.LogError("【MediaPipe】Failed to initialize CalculatorGraph.");
        //        yield break;
        //    }

        //    // ポーズランドマークの出力ストリームの設定
        //    var poseLandmarksStream = new OutputStream<NormalizedLandmarkListPacket, NormalizedLandmarkList>(_graph, "pose_landmarks");
        //    poseLandmarksStream.StartPolling().AssertOk();

        //    // sidePacket の作成とグラフの開始
        //    var sidePacket = new SidePacket();
        //    sidePacket.Emplace("model_complexity", new IntPacket((int)_modelComplexity));
        //    sidePacket.Emplace("input_rotation", new IntPacket(0));
        //    sidePacket.Emplace("input_horizontally_flipped", new BoolPacket(_isHorizontally_flipped));
        //    sidePacket.Emplace("input_vertically_flipped", new BoolPacket(_isVertically_flipped));
        //    sidePacket.Emplace("smooth_landmarks", new BoolPacket(true));
        //    sidePacket.Emplace("enable_segmentation", new BoolPacket(true));
        //    sidePacket.Emplace("smooth_segmentation", new BoolPacket(true));
        //    sidePacket.Emplace("output_rotation", new IntPacket(0));
        //    sidePacket.Emplace("output_horizontally_flipped", new BoolPacket(false));
        //    sidePacket.Emplace("output_vertically_flipped", new BoolPacket(false));

        //    _graph.StartRun(sidePacket).AssertOk();
        //    Debug.Log("【MediaPipe】Graph started successfully!");

        //    // タイマー開始
        //    var stopwatch = new Stopwatch();
        //    stopwatch.Start();

        //    var screenRect = _screen.GetComponent<RectTransform>().rect;

        //    while (true)
        //    {
        //        _inputTexture.SetPixels32(_webCamTexture.GetPixels32(_pixelData));
        //        var imageFrame = new ImageFrame(ImageFormat.Types.Format.Srgba, _width, _height, _width * 4, _inputTexture.GetRawTextureData<byte>());
        //        var currentTimestamp = stopwatch.ElapsedTicks / (System.TimeSpan.TicksPerMillisecond / 1000);

        //        _graph.AddPacketToInputStream("input_video", new ImageFramePacket(imageFrame, new Timestamp(currentTimestamp))).AssertOk();

        //        yield return new WaitForEndOfFrame();

        //        if (poseLandmarksStream.TryGetNext(out var LandMarks))
        //        {
        //            if (LandMarks == null) { continue; }

        //            landmarkList = LandMarks;
        //        }
        //        else
        //        {
        //            // Debug.LogWarning("【MediaPipe】No pose landmarks received.");
        //        }
        //    }
        //}
    }
}

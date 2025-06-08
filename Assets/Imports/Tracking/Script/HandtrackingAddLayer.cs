using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Mediapipe.Unity.CoordinateSystem;
using Stopwatch = System.Diagnostics.Stopwatch; // for Timestamp

namespace Mediapipe.Unity.Tutorial
{
    public class HandtrackingAddLayer : MonoBehaviour
    {
        [SerializeField] private RawImage _screen;
        [SerializeField] private int _width;
        [SerializeField] private int _height;
        [SerializeField] private int _fps;
        // ----以下追加-----
        // ハンドトラッキング用
        [SerializeField] private TextAsset _configAsset;
        private CalculatorGraph _graph;
        private ResourceManager _resourceManager;
        private enum ModelComplexity { Lite = 0, Full = 1, }
        [SerializeField] private ModelComplexity _modelComplexity = ModelComplexity.Full;
        [SerializeField] private int _maxNumHands = 2;
        // カメラ入力用
        private WebCamTexture _webCamTexture;
        private Texture2D _inputTexture;
        private Color32[] _pixelData;
        // 重ねるマーカー用
        [SerializeField] private MultiLandmarkListAnnotationController _annotationController;

        private IEnumerator Start()
        {
            // Webカメラの初期化を確認
            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogError("No webcam devices found!");
                yield break;  // Webカメラが見つからなければ処理を中断
            }

            var webCamDevice = WebCamTexture.devices[0];
            _webCamTexture = new WebCamTexture(webCamDevice.name, _width, _height, _fps);
            _webCamTexture.Play();
            Debug.Log("WebCamTexture is playing: " + _webCamTexture.isPlaying);

            // Webカメラの解像度が16以上になるまで待機
            yield return new WaitUntil(() => _webCamTexture.width > 16);
            if (_webCamTexture.width <= 16)
            {
                Debug.LogError("WebCamTexture did not initialize correctly.");
                yield break;
            }
            else
            {
                Debug.Log("WebCamTexture initialized successfully.");
            }

            // UIのセットアップ
            _screen.rectTransform.sizeDelta = new Vector2(_width, _height);
            _screen.texture = _webCamTexture;

            // _inputTextureと_pixelDataの初期化
            _inputTexture = new Texture2D(_width, _height, TextureFormat.RGBA32, false);
            _pixelData = new Color32[_width * _height];

            // リソースマネージャーの初期化
            //_resourceManager = new LocalResourceManager();
            _resourceManager = new StreamingAssetsResourceManager();

            // モデルの読み込み
            if (_modelComplexity == ModelComplexity.Lite)
            {
                Debug.Log("Loading Lite model...");
                yield return LoadModelAssets("hand_landmark_lite.bytes");
                yield return LoadModelAssets("hand_recrop.bytes");
                yield return LoadModelAssets("handedness.txt");
                yield return LoadModelAssets("palm_detection_lite.bytes");
            }
            else
            {
                Debug.Log("Loading Full model...");
                yield return LoadModelAssets("hand_landmark_full.bytes");
                yield return LoadModelAssets("hand_recrop.bytes");
                yield return LoadModelAssets("handedness.txt");
                yield return LoadModelAssets("palm_detection_full.bytes");
            }

            // _graphの初期化とエラーチェック
            if (_configAsset == null)
            {
                Debug.LogError("Configuration asset (_configAsset) is not assigned.");
                yield break;
            }
            _graph = new CalculatorGraph(_configAsset.text);
            if (_graph == null)
            {
                Debug.LogError("Failed to initialize CalculatorGraph.");
                yield break;
            }

            // handLandmarksStreamの設定
            var handLandmarksStream = new OutputStream<NormalizedLandmarkListVectorPacket, List<NormalizedLandmarkList>>(_graph, "hand_landmarks");
            handLandmarksStream.StartPolling().AssertOk();

            // sidePacketを作ってStartRunに渡す
            var sidePacket = new SidePacket();
            sidePacket.Emplace("model_complexity", new IntPacket((int)_modelComplexity));
            sidePacket.Emplace("num_hands", new IntPacket(_maxNumHands));
            sidePacket.Emplace("input_rotation", new IntPacket(0));
            sidePacket.Emplace("input_horizontally_flipped", new BoolPacket(false));
            sidePacket.Emplace("input_vertically_flipped", new BoolPacket(true));

            // Graphの開始
            _graph.StartRun(sidePacket).AssertOk();
            Debug.Log("Graph started successfully!");

            // タイマーの開始
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            // ランドマーク用
            var screenRect = _screen.GetComponent<RectTransform>().rect;

            while (true)
            {
                _inputTexture.SetPixels32(_webCamTexture.GetPixels32(_pixelData));
                var imageFrame = new ImageFrame(ImageFormat.Types.Format.Srgba, _width, _height, _width * 4, _inputTexture.GetRawTextureData<byte>());
                var currentTimestamp = stopwatch.ElapsedTicks / (System.TimeSpan.TicksPerMillisecond / 1000);

                _graph.AddPacketToInputStream("input_video", new ImageFramePacket(imageFrame, new Timestamp(currentTimestamp))).AssertOk();

                yield return new WaitForEndOfFrame();

                if (handLandmarksStream.TryGetNext(out var multiLandmarks))
                {
                    // マーカーの表示 (annotationControllerがnullでないことを確認)
                    if (_annotationController != null)
                    {
                        if (multiLandmarks != null && multiLandmarks.Count > 0)
                        {
                            _annotationController.DrawLandMark(multiLandmarks);
                            _annotationController.SetActiveLandMarks(true);
                            foreach (var landmarks in multiLandmarks)
                            {
                                var posTarget = landmarks.Landmark[8];
                                //Debug.Log($"Unity Local Coordinates: {screenRect.GetPoint(posTarget)}, Image Coordinates: {posTarget}");
                                if (screenRect.GetPoint(posTarget).x >= -20 && screenRect.GetPoint(posTarget).x <= 20)
                                {
                                    if (screenRect.GetPoint(posTarget).y <= 261 && screenRect.GetPoint(posTarget).y >= 221)
                                    {
                                        Debug.Log("OK!!!!!!");

                                    }
                                }
                            }
                        }
                        else
                        {
                            _annotationController.SetActiveLandMarks(false);  // マーカーを非表示にする
                        }
                    }
                    else
                    {
                        Debug.LogError("_annotationController is null, unable to draw landmarks.");
                    }
                }
                else
                {
                    Debug.LogWarning("No hand landmarks received.");
                    _annotationController?.SetActiveLandMarks(false);  // マーカーを非表示にする
                }
            }
        }

        // モデル読み込み用の補助メソッド
        private IEnumerator LoadModelAssets(string assetName)
        {
            yield return _resourceManager.PrepareAssetAsync(assetName);
            Debug.Log($"Loaded {assetName}");
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
                    Debug.Log("Graph disposed.");
                }
            }
        }
    }
}


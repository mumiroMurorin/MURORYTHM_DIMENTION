using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using Mediapipe;
using Mediapipe.Unity;
using Mediapipe.Unity.HandTracking;

namespace Mediapipe.Unity.Tutorial
{
    /// <summary>
    /// Hand tracking implementation that uses MediaPipe Unity's sample GraphRunner pipeline.
    /// The old HandTracking component is left untouched so both paths can be compared safely.
    /// </summary>
    public class HandTrackingWithGraphRunner : MonoBehaviour, ICameraInfoHolder
    {
        [Header("MediaPipe")]
        [SerializeField] private ConfigurableWebCamSource imageSource;
        [SerializeField] private TextureFramePool textureFramePool;
        [SerializeField] private HandTrackingGraph graphRunner;
        [SerializeField] private RunningMode runningMode = RunningMode.NonBlockingSync;

        [Header("Preview")]
        [SerializeField] private RawImage screen;

        [Header("Status")]
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private int fps;

        [Header("Performance")]
        [SerializeField, Min(1)] private int processFrameInterval = 1;

        [Header("Asset Loader")]
        [SerializeField] private bool provideStreamingAssetsResourceManager = true;

        private readonly ReactiveProperty<int> fpsReactiveProperty = new ReactiveProperty<int>();
        public IReadOnlyReactiveProperty<int> CameraFps => fpsReactiveProperty;

        private readonly ReactiveProperty<WebCamTexture> webCamTexture = new ReactiveProperty<WebCamTexture>();
        public IReadOnlyReactiveProperty<WebCamTexture> WebCamInfo => webCamTexture;

        private BodyTrackingSettings settings;
        private Coroutine initializeCoroutine;
        private Coroutine trackingCoroutine;
        private bool isReady;
        private bool isStartRequested;
        private bool isGraphRunning;

        private List<NormalizedLandmarkList> landmarkList;
        public List<NormalizedLandmarkList> LandmarkList => landmarkList;

        private List<ClassificationList> handednessList;
        public List<ClassificationList> HandednessList => handednessList;

        public int ResultVersion { get; private set; }

        public void Initialize(BodyTrackingSettings settings = default)
        {
            this.settings = settings;
            isStartRequested = false;

            StopRunningGraph();

            if (initializeCoroutine != null)
            {
                StopCoroutine(initializeCoroutine);
            }

            initializeCoroutine = StartCoroutine(InitializeCoroutine());
        }

        public void StartTracking()
        {
            isStartRequested = true;

            if (isReady && trackingCoroutine == null)
            {
                trackingCoroutine = StartCoroutine(TrackCoroutine());
            }
        }

        private IEnumerator InitializeCoroutine()
        {
            isReady = false;
            landmarkList = null;
            handednessList = null;
            ResultVersion = 0;

            if (!ValidateReferences()) { yield break; }

            PrepareAssetLoader();
            yield return null;

            if (WebCamTexture.devices.Length == 0)
            {
                Debug.LogError("[MediaPipe] No webcam devices found.");
                yield break;
            }

            yield return new WaitUntil(() => imageSource.sourceCandidateNames != null);
            imageSource.ApplySettings(settings);

            yield return imageSource.Play();
            if (!imageSource.isPrepared)
            {
                Debug.LogError("[MediaPipe] Failed to start WebCamSource.");
                yield break;
            }

            width = imageSource.textureWidth;
            height = imageSource.textureHeight;
            webCamTexture.Value = imageSource.GetCurrentTexture() as WebCamTexture;

            if (screen != null)
            {
                screen.rectTransform.sizeDelta = new Vector2(width, height);
                screen.texture = imageSource.GetCurrentTexture();
            }

            textureFramePool.ResizeTexture(width, height, TextureFormat.RGBA32);

            var graphInitRequest = graphRunner.WaitForInit(runningMode);
            yield return graphInitRequest;

            if (graphInitRequest.isError)
            {
                Debug.LogError($"[MediaPipe] Failed to initialize HandTrackingGraph: {graphInitRequest.error}");
                yield break;
            }

            graphRunner.StartRun(imageSource);
            isGraphRunning = true;
            isReady = true;

            if (isStartRequested && trackingCoroutine == null)
            {
                trackingCoroutine = StartCoroutine(TrackCoroutine());
            }
        }

        private IEnumerator TrackCoroutine()
        {
            var waitForEndOfFrame = new WaitForEndOfFrame();
            var lastFpsUpdateTime = Time.realtimeSinceStartup;
            var processedFrameCount = 0;

            while (isReady)
            {
                if (processFrameInterval > 1 && Time.frameCount % processFrameInterval != 0)
                {
                    yield return waitForEndOfFrame;
                    continue;
                }

                if (!textureFramePool.TryGetTextureFrame(out var textureFrame))
                {
                    yield return waitForEndOfFrame;
                    continue;
                }

                ReadFromImageSource(imageSource, textureFrame);
                graphRunner.AddTextureFrameToInputStream(textureFrame);

                yield return waitForEndOfFrame;

                if (TryReadLandmarks(out var handLandmarks, out var handedness))
                {
                    landmarkList = handLandmarks;
                    handednessList = handedness;
                    ResultVersion++;
                }

                processedFrameCount++;
                var now = Time.realtimeSinceStartup;
                var elapsed = now - lastFpsUpdateTime;
                if (elapsed < 1f) { continue; }

                fps = Mathf.RoundToInt(processedFrameCount / elapsed);
                if (fpsReactiveProperty.Value != fps)
                {
                    fpsReactiveProperty.Value = fps;
                }

                processedFrameCount = 0;
                lastFpsUpdateTime = now;
            }
        }

        private bool TryReadLandmarks(out List<NormalizedLandmarkList> handLandmarks, out List<ClassificationList> handedness)
        {
            var hasValue = graphRunner.TryGetNext(
                out _,
                out _,
                out handLandmarks,
                out _,
                out _,
                out handedness,
                false);

            return hasValue && handLandmarks != null;
        }

        private bool ValidateReferences()
        {
            if (imageSource == null)
            {
                Debug.LogError("[MediaPipe] ConfigurableWebCamSource is not assigned.");
                return false;
            }

            if (textureFramePool == null)
            {
                Debug.LogError("[MediaPipe] TextureFramePool is not assigned.");
                return false;
            }

            if (graphRunner == null)
            {
                Debug.LogError("[MediaPipe] HandTrackingGraph is not assigned.");
                return false;
            }

            return true;
        }

        private void PrepareAssetLoader()
        {
            if (!provideStreamingAssetsResourceManager) { return; }

            try
            {
                AssetLoader.Provide(new StreamingAssetsResourceManager());
            }
            catch (InvalidOperationException)
            {
                // Another MediaPipe path already initialized the global resource manager.
            }
        }

        private static void ReadFromImageSource(ImageSource source, TextureFrame textureFrame)
        {
            var sourceTexture = source.GetCurrentTexture();
            var textureType = sourceTexture.GetType();

            if (textureType == typeof(WebCamTexture))
            {
                textureFrame.ReadTextureFromOnCPU((WebCamTexture)sourceTexture);
            }
            else if (textureType == typeof(Texture2D))
            {
                textureFrame.ReadTextureFromOnCPU((Texture2D)sourceTexture);
            }
            else
            {
                textureFrame.ReadTextureFromOnCPU(sourceTexture);
            }
        }

        private void StopRunningGraph()
        {
            if (trackingCoroutine != null)
            {
                StopCoroutine(trackingCoroutine);
                trackingCoroutine = null;
            }

            if (isGraphRunning && graphRunner != null)
            {
                graphRunner.Stop();
                isGraphRunning = false;
            }

            if (imageSource != null && imageSource.isPrepared)
            {
                imageSource.Stop();
            }

            isReady = false;
        }

        private void OnDestroy()
        {
            if (initializeCoroutine != null)
            {
                StopCoroutine(initializeCoroutine);
                initializeCoroutine = null;
            }

            StopRunningGraph();
        }
    }
}

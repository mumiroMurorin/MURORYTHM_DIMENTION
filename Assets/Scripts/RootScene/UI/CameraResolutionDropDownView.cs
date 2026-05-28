using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UIInRootScene
{
    public class CameraResolutionDropDownView : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown dropdown;

        public Action<int, int> OnCameraResolutionChangedListener { get; set; }

        private readonly List<CameraResolutionOption> resolutionOptions = new List<CameraResolutionOption>();

        private static readonly CameraResolutionOption[] DefaultAvailableResolutions =
        {
            new CameraResolutionOption(176, 144, 30),
            new CameraResolutionOption(320, 240, 30),
            new CameraResolutionOption(424, 240, 30),
            new CameraResolutionOption(640, 360, 30),
            new CameraResolutionOption(640, 480, 30),
            new CameraResolutionOption(848, 480, 30),
            new CameraResolutionOption(960, 540, 30),
            new CameraResolutionOption(1280, 720, 30),
            new CameraResolutionOption(1600, 896, 30),
            new CameraResolutionOption(1920, 1080, 30),
        };

        private void Awake()
        {
            if (dropdown == null)
            {
                dropdown = GetComponent<TMP_Dropdown>();
            }

            if (dropdown == null)
            {
                return;
            }

            dropdown.onValueChanged.AddListener(OnValueChanged);
        }

        public void RefreshResolutions(int cameraIndex, int currentWidth = -1, int currentHeight = -1)
        {
            if (dropdown == null)
            {
                return;
            }

            resolutionOptions.Clear();
            dropdown.ClearOptions();

            CollectResolutions(cameraIndex, resolutionOptions);
            ApplyOptions(currentWidth, currentHeight);
        }

        public void OnChangeCameraResolution(int width, int height)
        {
            if (dropdown == null)
            {
                return;
            }

            var index = FindBestMatchIndex(width, height);
            if (index < 0)
            {
                return;
            }

            dropdown.SetValueWithoutNotify(index);
            dropdown.RefreshShownValue();
        }

        private void ApplyOptions(int currentWidth, int currentHeight)
        {
            if (resolutionOptions.Count == 0 && currentWidth > 0 && currentHeight > 0)
            {
                resolutionOptions.Add(new CameraResolutionOption(currentWidth, currentHeight, 0));
            }

            var optionLabels = new List<string>(resolutionOptions.Count);
            foreach (var option in resolutionOptions)
            {
                optionLabels.Add(option.Label);
            }

            dropdown.AddOptions(optionLabels);
            dropdown.interactable = resolutionOptions.Count > 0;

            var selectedIndex = FindBestMatchIndex(currentWidth, currentHeight);
            if (selectedIndex < 0 && resolutionOptions.Count > 0)
            {
                selectedIndex = 0;
            }

            if (selectedIndex >= 0)
            {
                dropdown.SetValueWithoutNotify(selectedIndex);
            }

            dropdown.RefreshShownValue();
        }

        private int FindBestMatchIndex(int width, int height)
        {
            if (resolutionOptions.Count == 0)
            {
                return -1;
            }

            if (width <= 0 || height <= 0)
            {
                return 0;
            }

            for (var i = 0; i < resolutionOptions.Count; i++)
            {
                var option = resolutionOptions[i];
                if (option.Width == width && option.Height == height)
                {
                    return i;
                }
            }

            return 0;
        }

        private void OnValueChanged(int index)
        {
            if (index < 0 || index >= resolutionOptions.Count)
            {
                return;
            }

            var option = resolutionOptions[index];
            if (option.Width <= 0 || option.Height <= 0)
            {
                return;
            }

            OnCameraResolutionChangedListener?.Invoke(option.Width, option.Height);
        }

        private static void CollectResolutions(int cameraIndex, List<CameraResolutionOption> buffer)
        {
            if (buffer == null)
            {
                return;
            }

            var devices = WebCamTexture.devices;
            if (devices == null || devices.Length == 0 || cameraIndex < 0 || cameraIndex >= devices.Length)
            {
                return;
            }

#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
            var resolutions = devices[cameraIndex].availableResolutions;
            if (resolutions != null && resolutions.Length > 0)
            {
                foreach (var resolution in resolutions)
                {
                    AddUnique(buffer, resolution.width, resolution.height, resolution.refreshRate);
                }

                return;
            }
#endif

            foreach (var resolution in DefaultAvailableResolutions)
            {
                AddUnique(buffer, resolution.Width, resolution.Height, resolution.FrameRate);
            }
        }

        private static void AddUnique(List<CameraResolutionOption> buffer, int width, int height, double frameRate)
        {
            for (var i = 0; i < buffer.Count; i++)
            {
                var option = buffer[i];
                if (option.Width == width && option.Height == height && Math.Abs(option.FrameRate - frameRate) < 0.01d)
                {
                    return;
                }
            }

            buffer.Add(new CameraResolutionOption(width, height, frameRate));
        }

        [Serializable]
        private class CameraResolutionOption
        {
            [SerializeField] private int width;
            [SerializeField] private int height;
            [SerializeField] private double frameRate;

            public int Width => width;
            public int Height => height;
            public double FrameRate => frameRate;
            public string Label => frameRate > 0 ? $"{width}x{height} ({frameRate:#.##}Hz)" : $"{width}x{height}";

            public CameraResolutionOption(int width, int height, double frameRate)
            {
                this.width = width;
                this.height = height;
                this.frameRate = frameRate;
            }
        }
    }
}

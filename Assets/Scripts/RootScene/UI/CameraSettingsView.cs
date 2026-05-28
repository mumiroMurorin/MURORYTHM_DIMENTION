using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UIInRootScene
{
    public class CameraSettingsView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI cameraNameTMP;
        [SerializeField] TextMeshProUGUI cameraResolutionTMP;
        [SerializeField] TextMeshProUGUI cameraFpsTMP;
        [SerializeField] CameraResolutionDropDownView cameraResolutionDropDown_view;
        [SerializeField] TMP_InputField inputFieldWidth;
        [SerializeField] TMP_InputField inputFieldHeight;
        [SerializeField] Button applyResolutionButton;
        [SerializeField] Button switchCameraButton;
        [SerializeField] Button flipHorizontalButton;
        [SerializeField] Button flipVerticalButton;
        [SerializeField] Button viewImageButton;

        public Action OnPushFlipHorizontalButtonListner { get; set; }
        public Action OnPushFlipVerticalButtonListner { get; set; }
        public Action OnPushViewImageButtonListner { get; set; }
        public Action<int, int> OnPushApplyResolutionButtonListener { get; set; }
        public Action<int, int> OnPushSelectResolutionListner { get; set; }
        public Action OnPushSwitchCameraButtonListner { get; set; }

        void Start()
        {
            if (cameraResolutionDropDown_view != null)
            {
                cameraResolutionDropDown_view.OnCameraResolutionChangedListener += OnPushResolutionDropdown;
            }

            viewImageButton?.onClick.AddListener(OnPushViewImageButton);
            flipHorizontalButton?.onClick.AddListener(OnPushFlipHorizontalButton);
            flipVerticalButton?.onClick.AddListener(OnPushFlipVerticalButton);
            applyResolutionButton?.onClick.AddListener(OnPushApplyResolutionButton);
            switchCameraButton?.onClick.AddListener(OnPushSwitchCameraButton);
        }

        public void OnChangeCameraInfo(WebCamTexture webCam)
        {
            if (webCam == null)
            {
                cameraNameTMP.text = "No Connection";
                cameraResolutionTMP.text = "-";
                return;
            }

            cameraNameTMP.text = webCam.deviceName;
            cameraResolutionTMP.text = $"{webCam.width} × {webCam.height}";
            OnChangeCameraResolution(webCam.width, webCam.height);
        }

        public void OnChangeFPS(int fps)
        {
            cameraFpsTMP.text = $"{fps}fps";
        }

        public void RefreshResolutionOptions(int cameraIndex, int currentWidth = -1, int currentHeight = -1)
        {
            cameraResolutionDropDown_view?.RefreshResolutions(cameraIndex, currentWidth, currentHeight);
        }

        public void OnChangeCameraResolution(int width, int height)
        {
            if (inputFieldWidth != null)
            {
                inputFieldWidth.text = width > 0 ? width.ToString() : string.Empty;
            }

            if (inputFieldHeight != null)
            {
                inputFieldHeight.text = height > 0 ? height.ToString() : string.Empty;
            }

            cameraResolutionDropDown_view?.OnChangeCameraResolution(width, height);
        }

        private void OnPushViewImageButton()
        {
            OnPushViewImageButtonListner?.Invoke();
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void OnPushFlipHorizontalButton()
        {
            OnPushFlipHorizontalButtonListner?.Invoke();
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void OnPushFlipVerticalButton()
        {
            OnPushFlipVerticalButtonListner?.Invoke();
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void OnPushApplyResolutionButton()
        {
            if (!int.TryParse(inputFieldWidth.text, out int width) || !int.TryParse(inputFieldHeight.text, out int height))
            {
                Debug.LogWarning($"フィールドに無効な値が入力されています: {inputFieldWidth.text}x{inputFieldHeight.text}");
                return;
            }

            OnPushApplyResolutionButtonListener?.Invoke(width, height);
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void OnPushResolutionDropdown(int width, int height)
        {
            OnPushSelectResolutionListner?.Invoke(width, height);
            EventSystem.current.SetSelectedGameObject(null);
        }

        private void OnPushSwitchCameraButton()
        {
            OnPushSwitchCameraButtonListner?.Invoke();
            EventSystem.current.SetSelectedGameObject(null);
        }
    }
}

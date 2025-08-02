using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.EventSystems;

namespace UIInRootScene
{
    public class CameraSettingsView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI cameraNameTMP;
        [SerializeField] TextMeshProUGUI cameraResolutionTMP;
        [SerializeField] TextMeshProUGUI cameraFpsTMP;
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
        public Action<int,int> OnPushApplyResolutionButtonListener { get; set; }
        public Action OnPushSwitchCameraButtonListner { get; set; }

        void Start()
        {
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
                // 名前の更新
                cameraNameTMP.text = "No Connection";

                // 解像度の更新
                cameraResolutionTMP.text = "-";
            }
            else
            {
                // 名前の更新
                cameraNameTMP.text = webCam.deviceName;

                // 解像度の更新
                cameraResolutionTMP.text = $"{webCam.width} × {webCam.height}";
                inputFieldWidth.text = webCam.width.ToString();
                inputFieldHeight.text = webCam.height.ToString();
            }
        }

        public void OnChangeFPS(int fps)
        {
            // fpsの更新
            cameraFpsTMP.text = $"{fps}fps";
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

        private void OnPushSwitchCameraButton()
        {
            OnPushSwitchCameraButtonListner?.Invoke();
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

}
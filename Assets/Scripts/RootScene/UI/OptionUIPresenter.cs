using System.Threading;
using Cysharp.Threading.Tasks;
using System;
using System.Linq;
using UniRx;
using UnityEngine;
using VContainer;
using TransitionerInRootScene;
using TransitionerInSelectScene;

namespace UIInRootScene
{
    public class OptionUIPresenter : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] HandInfoView handInfo_view;
        [SerializeField] TrackingModeDropDownView trackingModeDropDown_view;
        [SerializeField] ControllerPositionSettingView controllerLeftEdgeSetting_view;
        [SerializeField] ControllerPositionSettingView controllerRightEdgeSetting_view;
        [SerializeField] ControllerPositionSettingView controllerLowerCenterSetting_view;
        [SerializeField] CameraSettingsView cameraSettings_view;
        [SerializeField] GameObject cameraImage_view;
        [SerializeField] ButtonView backMusicSelectSceneButton_view;
        [SerializeField] ButtonView hiddenUIButton_view;
        [SerializeField] GameObject[] hiddenObjects;

        [Space(10)]
        [Header("Model")]
        [SerializeField] SerializeInterface<IPhaseTransitionableInRootScene> phaseTransitioner_model;
        [SerializeField] SerializeInterface<ISpaceInputHandler> spaceInputHandler_model;
        [SerializeField] SerializeInterface<ICameraInfoHolder> cameraInfo_model;

        IOptionGetter optionGetter_model;
        IOptionSetter optionSetter_model;
        ISpaceInputGetter spaceInputGetter_model;
        bool isHiddenUI;

        [Inject]
        public void Construct(IOptionGetter optionGetter, IOptionSetter optionSetter, ISpaceInputGetter spaceInputGetter)
        {
            optionGetter_model = optionGetter;
            optionSetter_model = optionSetter;
            spaceInputGetter_model = spaceInputGetter;
        }

        private void Start()
        {
            Bind();
            SetEvent();
            trackingModeDropDown_view?.OnChangeTrackingMode(optionGetter_model?.CurrentTrackingMode.Value ?? TrackingMode.BodyTracking);
            cameraSettings_view?.RefreshResolutionOptions(
                optionGetter_model?.TrackingSettings.CameraIndex ?? 0,
                optionGetter_model?.TrackingSettings.CameraWidth.Value ?? -1,
                optionGetter_model?.TrackingSettings.CameraHeight.Value ?? -1
            );
        }

        private void Bind()
        {
            // 正規化前の手の座標
            spaceInputHandler_model?.Value?.RightHandPos
                .Subscribe(handInfo_view.OnChangeRightHandOriginPosition)
                .AddTo(this.gameObject);

            spaceInputHandler_model?.Value?.LeftHandPos
                .Subscribe(handInfo_view.OnChangeLeftHandOriginPosition)
                .AddTo(this.gameObject);

            // 正規化された手の座標
            spaceInputGetter_model?.GetSpaceInput(SpaceTrackingTag.RightHand).ObserveAdd()
                .Subscribe(pos => { handInfo_view.OnChangeRightHandNormalizedPosition(pos.Value.Pos); })
                .AddTo(this.gameObject);

            spaceInputGetter_model?.GetSpaceInput(SpaceTrackingTag.LeftHand).ObserveAdd()
                .Subscribe(pos => { handInfo_view.OnChangeLeftHandNormalizedPosition(pos.Value.Pos); })
                .AddTo(this.gameObject);

            // ベクトル方向
            spaceInputGetter_model?.GetSpaceInputVelocity(SpaceTrackingTag.RightHand)
                .Subscribe(handInfo_view.OnChangeRightHandVelocity)
                .AddTo(this.gameObject);

            spaceInputGetter_model?.GetSpaceInputVelocity(SpaceTrackingTag.LeftHand)
                .Subscribe(handInfo_view.OnChangeLeftHandVelocity)
                .AddTo(this.gameObject);

            // コントローラ左端
            optionGetter_model?.TrackingSettings.ControllerLeftEdge
                .Subscribe(controllerLeftEdgeSetting_view.OnChangePosition)
                .AddTo(this.gameObject);

            // コントローラ右端
            optionGetter_model?.TrackingSettings.ControllerRightEdge
                .Subscribe(controllerRightEdgeSetting_view.OnChangePosition)
                .AddTo(this.gameObject);

            // コントローラ下
            optionGetter_model?.TrackingSettings.ControllerLowerCenter
                .Subscribe(controllerLowerCenterSetting_view.OnChangePosition)
                .AddTo(this.gameObject);

            // カメラ情報
            cameraInfo_model?.Value?.WebCamInfo
                .Subscribe(webCam =>
                {
                    cameraSettings_view.OnChangeCameraInfo(webCam);

                    if (webCam != null)
                    {
                        cameraSettings_view.RefreshResolutionOptions(
                            optionGetter_model?.TrackingSettings.CameraIndex ?? 0,
                            optionGetter_model?.TrackingSettings.CameraWidth.Value ?? webCam.width,
                            optionGetter_model?.TrackingSettings.CameraHeight.Value ?? webCam.height
                        );
                    }
                })
                .AddTo(this.gameObject);

            cameraInfo_model?.Value?.CameraFps
                .Subscribe(cameraSettings_view.OnChangeFPS)
                .AddTo(this.gameObject);

            // カメラ解像度
            if (optionGetter_model != null)
            {
                optionGetter_model.TrackingSettings.CameraWidth
                    .CombineLatest(
                        optionGetter_model.TrackingSettings.CameraHeight,
                        (width, height) => (width, height)
                    )
                    .Subscribe(size =>
                    {
                        cameraSettings_view.OnChangeCameraResolution(size.width, size.height);
                    })
                    .AddTo(this.gameObject);
            }

            // トラッキングモード
            optionGetter_model?.CurrentTrackingMode
                .Subscribe(trackingModeDropDown_view.OnChangeTrackingMode)
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {
            // 右手左手の反転
            handInfo_view.OnPushFlipButtonListner += () => { optionGetter_model?.TrackingSettings.SetIsHandFlipped(!optionGetter_model.TrackingSettings.IsHandFlipped.Value); };

            // コントローラ左端
            controllerLeftEdgeSetting_view.OnChangeValueListner += (pos) => { optionGetter_model?.TrackingSettings.SetControllerLeftEdge(pos); };
            controllerLeftEdgeSetting_view.OnPushSetRightPositionButtonListner += () => { optionGetter_model?.TrackingSettings.SetControllerLeftEdge(spaceInputHandler_model.Value.RightHandPos.Value); };
            controllerLeftEdgeSetting_view.OnPushSetLeftPositionButtonListner += () => { optionGetter_model?.TrackingSettings.SetControllerLeftEdge(spaceInputHandler_model.Value.LeftHandPos.Value); };

            // コントローラ右端
            controllerRightEdgeSetting_view.OnChangeValueListner += (pos) => { optionGetter_model?.TrackingSettings.SetControllerRightEdge(pos); };
            controllerRightEdgeSetting_view.OnPushSetRightPositionButtonListner += () => { optionGetter_model?.TrackingSettings.SetControllerRightEdge(spaceInputHandler_model.Value.RightHandPos.Value); };
            controllerRightEdgeSetting_view.OnPushSetLeftPositionButtonListner += () => { optionGetter_model?.TrackingSettings.SetControllerRightEdge(spaceInputHandler_model.Value.LeftHandPos.Value); };

            // コントローラ下
            controllerLowerCenterSetting_view.OnChangeValueListner += (pos) => { optionGetter_model?.TrackingSettings.SetControllerLowerCenter(pos); };
            controllerLowerCenterSetting_view.OnPushSetRightPositionButtonListner += () => { optionGetter_model?.TrackingSettings.SetControllerLowerCenter(spaceInputHandler_model.Value.RightHandPos.Value); };
            controllerLowerCenterSetting_view.OnPushSetLeftPositionButtonListner += () => { optionGetter_model?.TrackingSettings.SetControllerLowerCenter(spaceInputHandler_model.Value.LeftHandPos.Value); };

            // 入力映像の確認
            cameraSettings_view.OnPushViewImageButtonListner += () => { cameraImage_view.SetActive(!cameraImage_view.activeSelf); };

            // 入力映像の反転
            cameraSettings_view.OnPushFlipHorizontalButtonListner += () =>
            {
                optionGetter_model?.TrackingSettings.SetIsHorizontallyFlipped(!optionGetter_model.TrackingSettings.IsHorizontallyFlipped.Value);
                phaseTransitioner_model?.Value.TransitionPhase(PhaseStatusInRootScene.Reload);
            };

            cameraSettings_view.OnPushFlipVerticalButtonListner += () =>
            {
                optionGetter_model?.TrackingSettings.SetIsVerticallyFlipped(!optionGetter_model.TrackingSettings.IsVerticallyFlipped.Value);
                phaseTransitioner_model?.Value.TransitionPhase(PhaseStatusInRootScene.Reload);
            };

            // 解像度の変更(入力欄)
            cameraSettings_view.OnPushApplyResolutionButtonListener += (width, height) =>
            {
                optionGetter_model?.TrackingSettings.SetCameraWidth(width);
                optionGetter_model?.TrackingSettings.SetCameraHeight(height);
                cameraSettings_view.OnChangeCameraResolution(width, height);
                phaseTransitioner_model?.Value.TransitionPhase(PhaseStatusInRootScene.Reload);
            };

            // 解像度の変更(ドロップダウン)
            cameraSettings_view.OnPushSelectResolutionListner += (width, height) =>
            {
                optionGetter_model?.TrackingSettings.SetCameraWidth(width);
                optionGetter_model?.TrackingSettings.SetCameraHeight(height);
                cameraSettings_view.OnChangeCameraResolution(width, height);
                phaseTransitioner_model?.Value.TransitionPhase(PhaseStatusInRootScene.Reload);
            };

            // カメラの変更
            cameraSettings_view.OnPushSwitchCameraButtonListner += () =>
            {
                spaceInputHandler_model.Value.SwitchCamera();
                phaseTransitioner_model?.Value.TransitionPhase(PhaseStatusInRootScene.Reload);
            };

            // トラッキングモードの変更
            trackingModeDropDown_view.OnTrackingModeChangedListener += (mode) =>
            {
                optionSetter_model?.SetCurrentTrackingMode(mode);
                phaseTransitioner_model?.Value.TransitionPhase(PhaseStatusInRootScene.Reload);
            };

            // セレクトシーンに戻る
            backMusicSelectSceneButton_view.OnPushButtonListner += () => { phaseTransitioner_model?.Value.TransitionPhase(PhaseStatusInRootScene.TransitionSelectScene); };

            // UI非表示
            hiddenUIButton_view.OnPushButtonListner += () =>
            {
                isHiddenUI = !isHiddenUI;
                foreach (var obj in hiddenObjects)
                {
                    obj.SetActive(!isHiddenUI);
                }
            };
        }
    }
}

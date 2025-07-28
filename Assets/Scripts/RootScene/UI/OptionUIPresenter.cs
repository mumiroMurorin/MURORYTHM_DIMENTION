using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System.Linq;
using VContainer;
using TransitionerInSelectScene;
using Cysharp.Threading.Tasks;
using System.Threading;
using TransitionerInRootScene;

namespace UIInRootScene
{
    public class OptionUIPresenter : MonoBehaviour
    {
        [Header("View")]
        [SerializeField] HandInfoView handInfo_view;
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
        [SerializeField] SpaceInputHandlerForMediaPipe spaceInputHandler_model;
        [SerializeField] Mediapipe.Unity.Tutorial.BodyTracking cameraInfo_model;

        IOptionGetter optionGetter_model;
        ISpaceInputGetter spaceInputGetter_model;
        bool isHiddenUI;

        [Inject]
        public void Construct(IOptionGetter optionGetter, ISpaceInputGetter spaceInputGetter)
        {
            optionGetter_model = optionGetter;
            spaceInputGetter_model = spaceInputGetter;
        }

        private void Start()
        {
            Bind(); 
            SetEvent();
        }

        private void Bind()
        {
            // 正規化前の手の座標
            spaceInputHandler_model?.RightHandPos
                .Subscribe(handInfo_view.OnChangeRightHandOriginPosition)
                .AddTo(this.gameObject);

            spaceInputHandler_model?.LeftHandPos
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
            cameraInfo_model?.WebCamInfo
                .Subscribe(cameraSettings_view.OnChangeCameraInfo)
                .AddTo(this.gameObject);

            cameraInfo_model?.CameraFps
                .Subscribe(cameraSettings_view.OnChangeFPS)
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {
            // 右手左手の反転
            handInfo_view.OnPushFlipButtonListner += () => { optionGetter_model?.TrackingSettings.SetIsHandFlipped(!optionGetter_model.TrackingSettings.IsHandFlipped.Value); };

            // コントローラ左端
            controllerLeftEdgeSetting_view.OnChangeValueListner += (pos) => { optionGetter_model?.TrackingSettings.SetControllerLeftEdge(pos); };
            controllerLeftEdgeSetting_view.OnPushSetRightPositionButtonListner += () => { optionGetter_model?.TrackingSettings.SetControllerLeftEdge(spaceInputHandler_model.RightHandPos.Value); };
            controllerLeftEdgeSetting_view.OnPushSetLeftPositionButtonListner += () => { optionGetter_model?.TrackingSettings.SetControllerLeftEdge(spaceInputHandler_model.LeftHandPos.Value); };

            // コントローラ右端
            controllerRightEdgeSetting_view.OnChangeValueListner += (pos) => { optionGetter_model?.TrackingSettings.SetControllerRightEdge(pos); };
            controllerRightEdgeSetting_view.OnPushSetRightPositionButtonListner += () => { optionGetter_model?.TrackingSettings.SetControllerRightEdge(spaceInputHandler_model.RightHandPos.Value); };
            controllerRightEdgeSetting_view.OnPushSetLeftPositionButtonListner += () => { optionGetter_model?.TrackingSettings.SetControllerRightEdge(spaceInputHandler_model.LeftHandPos.Value); };

            // コントローラ下
            controllerLowerCenterSetting_view.OnChangeValueListner += (pos) => { optionGetter_model?.TrackingSettings.SetControllerLowerCenter(pos); };
            controllerLowerCenterSetting_view.OnPushSetRightPositionButtonListner += () => { optionGetter_model?.TrackingSettings.SetControllerLowerCenter(spaceInputHandler_model.RightHandPos.Value); };
            controllerLowerCenterSetting_view.OnPushSetLeftPositionButtonListner += () => { optionGetter_model?.TrackingSettings.SetControllerLowerCenter(spaceInputHandler_model.LeftHandPos.Value); };

            // 入力映像の確認
            cameraSettings_view.OnPushViewImageButtonListner += () => { cameraImage_view.SetActive(!cameraImage_view.activeSelf); };

            // 入力映像の反転
            cameraSettings_view.OnPushFlipHorizontalButtonListner += () => 
            { 
                optionGetter_model?.TrackingSettings.SetIsHorizontallyFlipped(!optionGetter_model.TrackingSettings.IsHorizontallyFlipped.Value);
                // リロード
                phaseTransitioner_model?.Value.TransitionPhase(PhaseStatusInRootScene.Reload);
            }; 
            
            cameraSettings_view.OnPushFlipVerticalButtonListner += () => 
            { 
                optionGetter_model?.TrackingSettings.SetIsVerticallyFlipped(!optionGetter_model.TrackingSettings.IsVerticallyFlipped.Value);
                // リロード
                phaseTransitioner_model?.Value.TransitionPhase(PhaseStatusInRootScene.Reload);
            };

            cameraSettings_view.OnPushApplyResolutionButtonListener += (width, height) =>
            {
                optionGetter_model?.TrackingSettings.SetCameraWidth(width);
                optionGetter_model?.TrackingSettings.SetCameraHeight(height);
                // リロード
                phaseTransitioner_model?.Value.TransitionPhase(PhaseStatusInRootScene.Reload);
            };


            // セレクトシーンに戻る
            backMusicSelectSceneButton_view.OnPushButtonListner += () => { phaseTransitioner_model?.Value.TransitionPhase(PhaseStatusInRootScene.TransitionSelectScene); };

            // UI非表示
            hiddenUIButton_view.OnPushButtonListner += () => 
            {
                isHiddenUI = !isHiddenUI;
                foreach(var obj in hiddenObjects)
                {
                    obj.SetActive(!isHiddenUI);
                }
            };
        }
    }

}
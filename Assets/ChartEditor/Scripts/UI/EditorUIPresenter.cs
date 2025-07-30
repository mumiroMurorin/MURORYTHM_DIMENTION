using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using Cysharp.Threading.Tasks;
using System.Threading;
using VContainer;
using System;

namespace ChartEditor
{
    public class EditorUIPresenter : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] ChartExtendButtonView chartExtendButton_view;
        [SerializeField] ChartShortenButtonView chartShortenButton_view;
        [SerializeField] MusicBrowseButtonView musicBrowseButton_view;
        [SerializeField] OffsetInputFieldView offsetInputField_view;
        [SerializeField] ChangeLaneDivNumButtonView changeLaneDivNumButton_view;
        [SerializeField] ScrollSensitivitySliderView scrollSensitivitySlider_view;
        [SerializeField] MusicNameView musicName_view;
        [SerializeField] RhythmConfigBarView rhythmConfigBar_view;
        [SerializeField] RhythmConfigSubView rhythmConfigSubDivision_view;
        [SerializeField] ImportButtonView importButton_view;
        [SerializeField] ExportButtonView exportButton_view;
        [SerializeField] OperationDescriptionView description_view;
        [SerializeField] ExplanationButtonView explanationButton_view;
        [SerializeField] ExplanationView explanation_view;
        [SerializeField] ScreenSizeDropDownView screenSizeDropDown_view;
        [SerializeField] SwitchLayerButtonView switchLayerButton_view;
        [Header("Models")]
        [SerializeField] ChartDataExporter chartDataExporter_model;
        [SerializeField] ChartDataImporter chartDataImporter_model;
        [SerializeField] ConfigEditor configEditor_model;
        [SerializeField] LaneExtender laneExtender_model;

        IChartEditorDataSetter dataSetter;
        IChartEditorDataGetter dataGetter_model;
        IChartEditorOptionSetter optionSetter;
        IChartEditorOptionGetter optionGetter;

        CancellationTokenSource soundLoadCts;

        [Inject]
        public void Construct(IChartEditorDataSetter chartEditorDataSetter, IChartEditorDataGetter chartEditorDataGetter, IChartEditorOptionSetter optionDataSetter, IChartEditorOptionGetter optionDataGetter)
        {
            dataSetter = chartEditorDataSetter;
            optionSetter = optionDataSetter;
            dataGetter_model = chartEditorDataGetter;
            optionGetter = optionDataGetter;
        }

        void Start()
        {
            BindForRhythmConfig();
            BindForOther();

            SetEvent();
        }

        private void BindForOther()
        {
            // 楽曲選択の可視不可視
            dataGetter_model?.PlayMode
                .Subscribe(musicBrowseButton_view.OnChangePlayMode)
                .AddTo(this.gameObject);

            // オフセットフィールドのインタラクト可不可
            // エクスポートフィールドのインタラクト可不可
            // 説明書ボタンのインタラクト可不可
            dataGetter_model?.PlayMode
                .Subscribe(value => { 
                    offsetInputField_view?.OnChangePlayMode(value);
                    exportButton_view?.OnChangePlayMode(value);
                    importButton_view?.OnChangePlayMode(value);
                    chartExtendButton_view?.OnChangePlayMode(value);
                    chartShortenButton_view?.OnChangePlayMode(value);
                    explanationButton_view?.OnChangePlayMode(value);
                })
                .AddTo(this.gameObject);

            // オフセットの変更
            dataGetter_model?.Offset
                .Subscribe(offsetInputField_view.OnChangeMainBPM)
                .AddTo(this.gameObject);

            // レーン分割数の変更
            optionGetter?.LaneDivisionNum
                .Subscribe(changeLaneDivNumButton_view.OnLaneDivNumChanged)
                .AddTo(this.gameObject);

            // スクロール感度
            optionGetter?.ScrollSensitivity
                .Subscribe(scrollSensitivitySlider_view.OnSensitivityChanged)
                .AddTo(this.gameObject);

            // 楽曲名の変更
            dataGetter_model?.Music
                .Subscribe(musicName_view.OnChangeMusic)
                .AddTo(this.gameObject);

            // 説明文の表示
            dataGetter_model?.CurrentEditMode
                .Subscribe(description_view.OnChangeEditMode)
                .AddTo(this.gameObject);

            // 説明書の表示、非表示
            dataGetter_model?.CurrentEditMode
                .Subscribe(explanation_view.OnChangeEditMode)
                .AddTo(this.gameObject);

            // 解像度の変更
            optionGetter?.Resolution
                .Subscribe(screenSizeDropDown_view.OnChangeResolution)
                .AddTo(this.gameObject);

            // レイヤー変更ボタンのインタラクト可不可
            dataGetter_model?.CurrentEditMode
                .Subscribe(switchLayerButton_view.OnChangeEditMode)
                .AddTo(this.gameObject);

            // レイヤー変更ボタンの更新
            dataGetter_model?.EditNoteType
                .Subscribe(switchLayerButton_view.OnChangeEditNoteType)
                .AddTo(this.gameObject);
        }

        private void BindForRhythmConfig()
        {
            // リズムコンフィグ(小節線)のクリック
            dataGetter_model?.CurrentEditMode
                .Where(mode => mode == EditMode.EditingBarConfig)
                .Subscribe(value =>
                {
                    var config = configEditor_model.BarConfig.Value.BarDataGetter.BarConfig;


                    rhythmConfigBar_view.SetDataOnUI(config);
                    rhythmConfigBar_view.SetActive(true);
                })
                .AddTo(this.gameObject);

            // リズムコンフィグ(分線)のクリック
            dataGetter_model?.CurrentEditMode
                .Where(mode => mode == EditMode.EditingSubDivisionConfig)
                .Subscribe(value =>
                {
                    var config = configEditor_model.SubDivisionConfig.Value.SubDivisionDataGetter.SubConfig;
                    rhythmConfigSubDivision_view.SetDataOnUI(config);
                    rhythmConfigSubDivision_view.SetActive(true);
                })
                .AddTo(this.gameObject);

            // リズムコンフィグ(小節線)を閉じる
            dataGetter_model?.CurrentEditMode
                .Pairwise()
                .Where(pair => pair.Previous == EditMode.EditingBarConfig)
                .Subscribe(value =>
                {
                    rhythmConfigBar_view.SetData(configEditor_model.ChangeBarConfig);
                    rhythmConfigBar_view.SetActive(false);
                })
                .AddTo(this.gameObject);

            // リズムコンフィグ(分線)を閉じる
            dataGetter_model?.CurrentEditMode
                .Pairwise()
                .Where(pair => pair.Previous == EditMode.EditingSubDivisionConfig)
                .Subscribe(value =>
                {
                    rhythmConfigSubDivision_view.SetData(configEditor_model.ChangeSubDivisionConfig);
                    rhythmConfigSubDivision_view.SetActive(false);
                })
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {
            // 曲選択ボタン
            musicBrowseButton_view.OnClickedListner += BrowseAudioFile;

            // オフセットフィールド
            offsetInputField_view.OnValueChangedListner += dataSetter.SetOffset;

            // レーン分割数
            changeLaneDivNumButton_view.OnButtonClickedListener += () => optionSetter.SetLaneDivisionNum(true);

            // スクロール感度
            scrollSensitivitySlider_view.OnSliderChangedListener += optionSetter.SetScrollSensitivity;

            // エクスポートボタン
            exportButton_view.OnClickedListner += chartDataExporter_model.Export;

            // インポートボタン
            importButton_view.OnClickedListner += chartDataImporter_model.Import;

            // 譜面延長ボタン
            chartExtendButton_view.OnClickedListner += () => laneExtender_model.ChangeChartLength(1);

            // 譜面縮小ボタン
            chartShortenButton_view.OnClickedListner += () => laneExtender_model.ChangeChartLength(-1);

            // 解像度変更ボタン
            screenSizeDropDown_view.OnChangeValueListner += (resolution) => optionSetter.SetResolution(resolution);

            // リズムコンフィグ
            rhythmConfigBar_view.OnClickedApplyButtonListner += () => configEditor_model.CloseConfig();
            rhythmConfigSubDivision_view.OnClickedApplyButtonListner += () => configEditor_model.CloseConfig();

            // レイヤー変更ボタン
            switchLayerButton_view.OnClickCloseButtonListner += () => {
                dataSetter.SwitchEditNoteType();
            };

            explanationButton_view.OnClickedListner += () => dataSetter.SetEditMode(EditMode.Explanation);

            // 説明書を閉じる
            explanation_view.OnClickCloseButtonListner += () => dataSetter.SetEditMode(EditMode.None);
        }

        /// <summary>
        /// 楽曲ファイルをセット
        /// </summary>
        private async void BrowseAudioFile()
        {
            if (soundLoadCts != null)
            {
                soundLoadCts.Cancel();  
                soundLoadCts.Dispose();
            }

            soundLoadCts = new CancellationTokenSource();

            AudioClip clip = await AudioFileSelector.SelectAudioFile(soundLoadCts.Token);
            dataSetter.SetMusic(clip);
        }

        private void OnDestroy()
        {
            if (soundLoadCts != null)
            {
                soundLoadCts.Cancel();
                soundLoadCts.Dispose();
                soundLoadCts = null;
            }
        }
    }
}

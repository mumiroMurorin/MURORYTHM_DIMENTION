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
        [SerializeField] List<ToolButtonToEditMode> toolButtons_view;
        [SerializeField] List<NoteButtonToEditMode> noteButtons_view;
        [SerializeField] NotesViewportView notesViewport_view;
        [SerializeField] ChartExtendButtonView chartExtendButton_view;
        [SerializeField] ChartShortenButtonView chartShortenButton_view;
        [SerializeField] MusicBrowseButtonView musicBrowseButton_view;
        [SerializeField] OffsetInputFieldView offsetInputField_view;
        [SerializeField] ChangeLaneDivNumButtonView changeLaneDivNumButton_view;
        [SerializeField] ScrollSensitivitySliderView scrollSensitivitySlider_view;
        [SerializeField] MusicNameView musicName_view;
        [SerializeField] AutoEditModeButtonView autoEditModeButton_view;
        [SerializeField] RhythmConfigBarView rhythmConfigBar_view;
        [SerializeField] RhythmConfigSubView rhythmConfigSubDivision_view;
        [SerializeField] ImportButtonView importButton_view;
        [SerializeField] ExportButtonView exportButton_view;
        [SerializeField] OperationDescriptionView description_view;
        [SerializeField] ExplanationButtonView explanationButton_view;
        [SerializeField] ExplanationView explanation_view;
        [Header("Models")]
        [SerializeField] ChartDataExporter chartDataExporter_model;
        [SerializeField] ChartDataImporter chartDataImporter_model;

        IChartEditorDataSetter editorDataSetter_model;
        IChartEditorDataGetter editorDataGetter_model;
        IChartEditorOptionSetter optionDataSetter_model;
        IChartEditorOptionGetter optionDataGetter_model;

        CancellationTokenSource soundLoadCts;

        [Inject]
        public void Construct(IChartEditorDataSetter chartEditorDataSetter, IChartEditorDataGetter chartEditorDataGetter, IChartEditorOptionSetter optionDataSetter, IChartEditorOptionGetter optionDataGetter)
        {
            editorDataSetter_model = chartEditorDataSetter;
            optionDataSetter_model = optionDataSetter;
            editorDataGetter_model = chartEditorDataGetter;
            optionDataGetter_model = optionDataGetter;
        }

        void Start()
        {
            BindForRhythmConfig();
            BindForEditView();
            BindForOther();

            SetEvent();
        }

        private void BindForOther()
        {
            // 楽曲選択の可視不可視
            editorDataGetter_model?.PlayMode
                .Subscribe(musicBrowseButton_view.OnChangePlayMode)
                .AddTo(this.gameObject);

            // オフセットフィールドのインタラクト可不可
            // エクスポートフィールドのインタラクト可不可
            // 説明書ボタンのインタラクト可不可
            editorDataGetter_model?.PlayMode
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
            editorDataGetter_model?.Offset
                .Subscribe(offsetInputField_view.OnChangeMainBPM)
                .AddTo(this.gameObject);

            // レーン分割数の変更
            optionDataGetter_model?.LaneDivisionNum
                .Subscribe(changeLaneDivNumButton_view.OnLaneDivNumChanged)
                .AddTo(this.gameObject);

            // スクロール感度
            optionDataGetter_model?.ScrollSensitivity
                .Subscribe(scrollSensitivitySlider_view.OnSensitivityChanged)
                .AddTo(this.gameObject);

            // 楽曲名の変更
            editorDataGetter_model?.Music
                .Subscribe(musicName_view.OnChangeMusic)
                .AddTo(this.gameObject);

            // 説明文の表示
            editorDataGetter_model?.CurrentEditMode
                .Subscribe(description_view.OnChangeEditMode)
                .AddTo(this.gameObject);

            // 説明書の表示、非表示
            editorDataGetter_model?.CurrentEditMode
                .Subscribe(explanation_view.OnChangeEditMode)
                .AddTo(this.gameObject);
        }

        private void BindForRhythmConfig()
        {
            // リズムコンフィグ(小節線)のクリック
            editorDataGetter_model?.RhythmConfigurableBar
                .Where(value => value != null)
                .Subscribe(value =>
                {
                    BarDataInChart data = value.BarDataGetter.BarData;
                    rhythmConfigBar_view.SetDataOnUI(data.BeatCount.Value, data.BeatUnit.Value, data.DivisionNum.Value);
                    rhythmConfigBar_view.SetActive(true);
                })
                .AddTo(this.gameObject);

            // リズムコンフィグ(分線)のクリック
            editorDataGetter_model?.RhythmConfigurableSubDivision
                .Where(value => value != null)
                .Subscribe(value =>
                {
                    SubDivisionDataInBeat data = value.SubDivisionDataGetter.SubDivisionData;
                    rhythmConfigSubDivision_view.SetDataOnUI(data.Bpm.Value);
                    rhythmConfigSubDivision_view.SetActive(true);
                })
                .AddTo(this.gameObject);

            // リズムコンフィグ(小節線)を閉じる
            editorDataGetter_model?.RhythmConfigurableBar
                .Pairwise()
                .Where(value => value.Current == null)
                .Subscribe(value =>
                {
                    rhythmConfigBar_view.SetData(value.Previous.BarDataGetter.BarData);
                    rhythmConfigBar_view.SetActive(false);
                })
                .AddTo(this.gameObject);

            // リズムコンフィグ(分線)を閉じる
            editorDataGetter_model?.RhythmConfigurableSubDivision
                .Pairwise()
                .Where(value => value.Current == null)
                .Subscribe(value =>
                {
                    rhythmConfigSubDivision_view.SetData(value.Previous.SubDivisionDataGetter.SubDivisionData, editorDataGetter_model.ChartData.Value);
                    rhythmConfigSubDivision_view.SetActive(false);
                })
                .AddTo(this.gameObject);
        }

        private void BindForEditView()
        {
            // ツールボタン
            foreach (var button in toolButtons_view)
            {
                button.BindForDeploymentNoteType(editorDataGetter_model.CurrentEditMode, this.gameObject);
                button.BindForAutomode(editorDataGetter_model.AutoEditMode, this.gameObject);
            }

            // ノートボタン
            foreach (var button in noteButtons_view)
            {
                button.Bind(editorDataGetter_model.DeploymentNoteType, this.gameObject);
            }

            // ノーツビューの可視不可視
            editorDataGetter_model?.CurrentEditMode
                .Subscribe(notesViewport_view.OnChangeEditMode)
                .AddTo(this.gameObject);

            // オートエディットモードの変更
            editorDataGetter_model?.AutoEditMode
                .Subscribe(autoEditModeButton_view.OnChangeAutoEditMode)
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {
            // ツールボタン
            foreach (var button in toolButtons_view)
            {
                button.SetEvent(() => { editorDataSetter_model.SetEditMode(button.EditMode); });
            }

            // ノーツボタン
            foreach(var button in noteButtons_view)
            {
                button.SetEvent(() => { editorDataSetter_model.SetNoteType(button.NoteType); });
            }

            // 曲選択ボタン
            musicBrowseButton_view.OnClickedListner += BrowseAudioFile;

            // オートエディットモードボタン
            autoEditModeButton_view.OnClickedListner += () =>
            {
                bool currentMode = editorDataGetter_model.AutoEditMode.Value;
                editorDataSetter_model.SetAutoEditMode(!currentMode);
            };

            // オフセットフィールド
            offsetInputField_view.OnValueChangedListner += editorDataSetter_model.SetOffset;

            // レーン分割数
            changeLaneDivNumButton_view.OnButtonClickedListener += () => optionDataSetter_model.SetLaneDivisionNum(true);

            // スクロール感度
            scrollSensitivitySlider_view.OnSliderChangedListener += optionDataSetter_model.SetScrollSensitivity;

            // エクスポートボタン
            exportButton_view.OnClickedListner += chartDataExporter_model.Export;

            // インポートボタン
            importButton_view.OnClickedListner += chartDataImporter_model.Import;

            // 譜面延長ボタン
            chartExtendButton_view.OnClickedListner += () => editorDataSetter_model.ChangeChartLength(1);

            // 譜面縮小ボタン
            chartShortenButton_view.OnClickedListner += () => editorDataSetter_model.ChangeChartLength(-1);

            // リズムコンフィグ
            rhythmConfigBar_view.OnClickedApplyButtonListner += () => CloseConfig();
            rhythmConfigSubDivision_view.OnClickedApplyButtonListner += () => CloseConfig();

            explanationButton_view.OnClickedListner += () => { editorDataSetter_model.SetEditMode(EditMode.Explanation); };

            // 説明書を閉じる
            explanation_view.OnClickCloseButtonListner += () => { editorDataSetter_model.SetEditMode(EditMode.None); };
        }

        private void CloseConfig()
        {
            editorDataSetter_model.SetRhythmConfigurableBar(null);
            editorDataSetter_model.SetRhythmConfigurableSubDivision(null);

            editorDataSetter_model.SetEditMode(EditMode.None);
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
            editorDataSetter_model.SetMusic(clip);
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

        #region その他まとめクラス

        /// <summary>
        /// ツールボタンに対するアクション他
        /// </summary>
        [Serializable]
        public class ToolButtonToEditMode
        {
            [SerializeField] ChangeEditModeButtonView toolButton_view;
            [SerializeField] EditMode editMode;
            [SerializeField] bool IsHiddenInAutoMode;

            public ChangeEditModeButtonView ToolButton_view { get { return toolButton_view; } }

            public EditMode EditMode { get { return editMode; } }

            public void BindForDeploymentNoteType(IReadOnlyReactiveProperty<EditMode> reactiveProperty, GameObject gameObject)
            {
                reactiveProperty
                    .Subscribe(editMode => ToolButton_view.OnChangeEditMode(editMode == this.editMode))
                    .AddTo(gameObject);
            }

            public void BindForAutomode(IReadOnlyReactiveProperty<bool> isAutoModeRP, GameObject gameObject)
            {
                isAutoModeRP
                    .Subscribe(isAutomode => ToolButton_view.OnChangeAutoMode(isAutomode && IsHiddenInAutoMode))
                    .AddTo(gameObject);
            }

            public void SetEvent(Action action)
            {
                toolButton_view.OnClickedListner += action;
            }
        }

        /// <summary>
        /// ノートボタンに対するアクション他
        /// </summary>
        [Serializable]
        public class NoteButtonToEditMode
        {
            [SerializeField] ChangeDeploymentNoteButtonView noteButton_view;
            [SerializeField] DeploymentNoteType noteType;

            public ChangeDeploymentNoteButtonView NoteButton_view { get { return noteButton_view; } }

            public DeploymentNoteType NoteType { get { return noteType; } }

            public void Bind(IReadOnlyReactiveProperty<DeploymentNoteType> reactiveProperty, GameObject gameObject)
            {
                reactiveProperty
                    .Subscribe(noteType => NoteButton_view.OnChangeDeploymentNote(noteType == this.noteType))
                    .AddTo(gameObject);
            }

            public void SetEvent(Action action)
            {
                noteButton_view.OnClickedListner += action;
            }
        }

        #endregion
    }
}

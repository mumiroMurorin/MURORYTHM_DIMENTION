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
        [SerializeField] List<ToolButtonToEditMode> toolButtons_view;
        [SerializeField] List<NoteButtonToEditMode> noteButtons_view;
        [SerializeField] NotesViewportView notesViewport_view;
        [SerializeField] MusicBrowseButtonView musicBrowseButton_view;
        [SerializeField] BPMInputFieldView bpmInputField_view;
        [SerializeField] OffsetInputFieldView offsetInputField_view;
        [SerializeField] MusicNameView musicName_view;
        [SerializeField] RhythmConfigBarView rhythmConfigBar_view;
        [SerializeField] RhythmConfigSubView rhythmConfigSubDivision_view;

        AudioFileSelector audioFileSelector = new AudioFileSelector();

        IChartEditorDataSetter dataSetter_model;
        IChartEditorDataGetter dataGetter_model;

        CancellationTokenSource soundLoadCts;

        [Inject]
        public void Construct(IChartEditorDataSetter chartEditorDataSetter, IChartEditorDataGetter chartEditorDataGetter)
        {
            dataSetter_model = chartEditorDataSetter;
            dataGetter_model = chartEditorDataGetter;
        }

        void Start()
        {
            Bind(); 
            SetEvent();
        }

        private void Bind()
        {
            // ツールボタン
            foreach(var button in toolButtons_view)
            {
                button.Bind(dataGetter_model.CurrentEditMode, this.gameObject);
            }

            // ノートボタン
            foreach (var button in noteButtons_view)
            {
                button.Bind(dataGetter_model.DeploymentNoteType, this.gameObject);
            }

            // ノーツビューの可視不可視
            dataGetter_model?.CurrentEditMode
                .Subscribe(notesViewport_view.OnChangeEditMode)
                .AddTo(this.gameObject);

            // 楽曲選択の可視不可視
            dataGetter_model?.PlayMode
                .Subscribe(musicBrowseButton_view.OnChangePlayMode)
                .AddTo(this.gameObject);

            // bpmフィールドの可視不可視
            dataGetter_model?.PlayMode
                .Subscribe(bpmInputField_view.OnChangePlayMode)
                .AddTo(this.gameObject);

            // オフセットフィールドの可視不可視
            dataGetter_model?.PlayMode
                .Subscribe(offsetInputField_view.OnChangePlayMode)
                .AddTo(this.gameObject);

            // 楽曲名の変更
            dataGetter_model?.Music
                .Subscribe(musicName_view.OnChangeMusic)
                .AddTo(this.gameObject);

            // リズムコンフィグ(小節線)のクリック
            dataGetter_model?.RhythmConfigurableBar
                .Where(value => value != null)
                .Subscribe(value =>
                {
                    BarDataInChart data = value.BarDataGetter.BarData;
                    rhythmConfigBar_view.SetDataOnUI(data.BeatCount.Value, data.BeatUnit.Value, data.DivisionNum.Value);
                    rhythmConfigBar_view.SetActive(true);
                })
                .AddTo(this.gameObject);

            // リズムコンフィグ(分線)のクリック
            dataGetter_model?.RhythmConfigurableSubDivision
                .Where(value => value != null)
                .Subscribe(value =>
                {
                    SubDivisionDataInBeat data = value.SubDivisionDataGetter.SubDivisionData;
                    rhythmConfigSubDivision_view.SetDataOnUI(data.Bpm.Value);
                    rhythmConfigSubDivision_view.SetActive(true);
                })
                .AddTo(this.gameObject);

            // リズムコンフィグ(小節線)を閉じる
            dataGetter_model?.RhythmConfigurableBar
                .Pairwise()
                .Where(value => value.Current == null)
                .Subscribe(value =>
                {
                    rhythmConfigBar_view.SetData(value.Previous.BarDataGetter.BarData);
                    rhythmConfigBar_view.SetActive(false);
                })
                .AddTo(this.gameObject);

            // リズムコンフィグ(分線)を閉じる
            dataGetter_model?.RhythmConfigurableSubDivision
                .Pairwise()
                .Where(value => value.Current == null)
                .Subscribe(value =>
                {
                    rhythmConfigSubDivision_view.SetData(value.Previous.SubDivisionDataGetter.SubDivisionData);
                    rhythmConfigSubDivision_view.SetActive(false);
                })
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {
            // ツールボタン
            foreach (var button in toolButtons_view)
            {
                button.SetEvent(() => { dataSetter_model.SetEditMode(button.EditMode); });
            }

            // ノーツボタン
            foreach(var button in noteButtons_view)
            {
                button.SetEvent(() => { dataSetter_model.SetNoteType(button.NoteType); });
            }

            // 曲選択ボタン
            musicBrowseButton_view.OnClickedListner += BrowseAudioFile;

            // bpm変更フィールド
            bpmInputField_view.OnValueChangedListner += (value) => 
            { 
                dataSetter_model.SetMainBpm(value);
                dataSetter_model.InitializeChartData();
            };

            // オフセットフィールド
            offsetInputField_view.OnValueChangedListner += dataSetter_model.SetOffset;

            // リズムコンフィグ
            rhythmConfigBar_view.OnClickedApplyButtonListner += () => dataSetter_model.SetRhythmConfigurableBar(null);
            rhythmConfigSubDivision_view.OnClickedApplyButtonListner += () => dataSetter_model.SetRhythmConfigurableSubDivision(null);
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

            AudioClip clip = await audioFileSelector.SelectAudioFile(soundLoadCts.Token);
            dataSetter_model.SetMusic(clip);
            dataSetter_model.InitializeChartData();
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

            public ChangeEditModeButtonView ToolButton_view { get { return toolButton_view; } }

            public EditMode EditMode { get { return editMode; } }

            public void Bind(IReadOnlyReactiveProperty<EditMode> reactiveProperty, GameObject gameObject)
            {
                reactiveProperty
                    .Subscribe(editMode => ToolButton_view.OnChangeEditMode(editMode == this.editMode))
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

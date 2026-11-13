using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class PreviewUIPresenter : MonoBehaviour
    {
        [Header("Views")]
        [SerializeField] ButtonView previewRefreshButton_view;
        [SerializeField] ButtonView previewStartButton_view;
        [SerializeField] ButtonView backEditorButton_view;
        [SerializeField] OffsetInputFieldView offsetInputField_view;
        [SerializeField] ChangeLaneDivNumButtonView changeLaneDivNumButton_view;
        [SerializeField] NoteSpeedSliderView noteSpeedSlider_view;

        [Space(20)]
        [Header("Models")]
        [SerializeField] MonoBehaviour previewRefreshTarget;

        IChartEditorDataGetter dataGetter_model;
        IChartEditorDataSetter dataSetter_model;
        IChartEditorOptionSetter optionSetter_model;
        IChartEditorOptionGetter optionGetter_model;
        INoteSpawnDataOptionSetter noteSpawnDataOptionSetter_model;
        INoteSpawnDataOptionGetter noteSpawnDataOptionGetter_model;
        INotesDataGetter notesGetter_model;

        IChartPreviewRefreshable previewRefreshable;
        EditNoteType editNoteTypeCache = EditNoteType.Ground;

        [Inject]
        public void Construct(
            IChartEditorDataGetter dataGetter,
            IChartEditorDataSetter dataSetter,
            IChartEditorOptionSetter optionDataSetter,
            IChartEditorOptionGetter optionDataGetter,
            INoteSpawnDataOptionSetter noteSpawnDataOptionSetter,
            INoteSpawnDataOptionGetter noteSpawnDataOptionGetter,
            INotesDataGetter notesGetter)
        {
            this.notesGetter_model = notesGetter;
            this.dataGetter_model = dataGetter;
            this.dataSetter_model = dataSetter;
            this.optionSetter_model = optionDataSetter;
            this.optionGetter_model = optionDataGetter;
            this.noteSpawnDataOptionSetter_model = noteSpawnDataOptionSetter;
            this.noteSpawnDataOptionGetter_model = noteSpawnDataOptionGetter;
        }

        private void Start()
        {
            previewRefreshable = previewRefreshTarget as IChartPreviewRefreshable;

            Bind();
            SetEvent();

            // 再生速度・ノーツ音の切り替え行を、レーン分割数の行の下に追加する
            SettingsCycleRow.CreateEditorRows(changeLaneDivNumButton_view, optionGetter_model, optionSetter_model);
        }

        private void Bind()
        {
            dataGetter_model?.Offset
                .Subscribe(offsetInputField_view.OnChangeFloatValue)
                .AddTo(this.gameObject);

            optionGetter_model?.LaneDivisionNum
                .Subscribe(changeLaneDivNumButton_view.OnLaneDivNumChanged)
                .AddTo(this.gameObject);

            noteSpawnDataOptionGetter_model?.NoteSpeed
                .Subscribe(noteSpeedSlider_view.OnValueChanged)
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {
            if (previewRefreshButton_view != null)
            {
                // 更新ボタンは、変わっていないノーツも含めてすべて作り直す
                // (普段は編集に合わせて自動で反映されるので、表示がおかしいときの作り直し用)
                previewRefreshButton_view.OnPushButtonListner += () => previewRefreshable?.RebuildPreviewFromEditorData();
            }

            if (previewStartButton_view != null)
            {
                previewStartButton_view.OnPushButtonListner += () =>
                {
                    RunStartPreviewFlowAsync().Forget();
                };
            }

            if (backEditorButton_view != null)
            {
                backEditorButton_view.OnPushButtonListner += () =>
                {
                    ChangeEditMode(EditMode.None);
                    ChangeEditNoteType(GetReturnLayer());
                };
            }

            offsetInputField_view.OnFloatValueChangedListner += dataSetter_model.SetOffset;
            changeLaneDivNumButton_view.OnPushButtonListner += () => optionSetter_model.SetLaneDivisionNum(true);

            // ノートスピード変更: スライダーを動かし終えたら反映する
            // (プレビューは、ノートスピードの変化を見て自動で並べ直される)
            noteSpeedSlider_view.OnNoteSpeedChangedListener += noteSpawnDataOptionSetter_model.SetNoteSpeed;
        }

        /// <summary>
        /// 全画面プレビューから編集画面へ戻るときに開くレイヤーを指定する
        /// (全画面プレビューの中でノーツを選んだとき、そのノーツのあるレイヤーへ戻れるように)
        /// </summary>
        public void SetReturnLayer(EditNoteType layer)
        {
            if (layer != EditNoteType.Ground && layer != EditNoteType.Space) { return; }

            editNoteTypeCache = layer;
        }

        /// <summary>
        /// 編集画面へ戻るときに開くレイヤー
        /// </summary>
        private EditNoteType GetReturnLayer()
        {
            // 頂点レイヤーから全画面プレビューに来た後、編集していたノーツがUndoなどで無くなっていることがある
            // 編集するノーツが無いまま頂点レイヤーを開くと操作でエラーになるので、スペースレイヤーへ戻す
            if (editNoteTypeCache == EditNoteType.Vertices &&
                (notesGetter_model == null || notesGetter_model.EditingVertices.Value == null))
            {
                return EditNoteType.Space;
            }

            return editNoteTypeCache;
        }

        private async UniTaskVoid RunStartPreviewFlowAsync()
        {
            editNoteTypeCache = dataGetter_model.EditNoteType.Value;

            if (previewRefreshable != null)
            {
                await previewRefreshable.RefreshPreviewFromEditorDataAsync();
            }

            ChangeEditMode(EditMode.Preview);
            ChangeEditNoteType(EditNoteType.Preview);
        }

        private void ChangeEditMode(EditMode mode)
        {
            dataSetter_model?.SetEditMode(mode);
        }

        private void ChangeEditNoteType(EditNoteType type)
        {
            dataSetter_model?.SetEditNoteType(type);
        }
    }
}

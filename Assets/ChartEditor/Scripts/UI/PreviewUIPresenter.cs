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
        INotesDataSetter notesSetter_model;

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
            INotesDataGetter notesGetter,
            INotesDataSetter notesSetter)
        {
            this.notesGetter_model = notesGetter;
            this.notesSetter_model = notesSetter;
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
                previewRefreshButton_view.OnPushButtonListner += RebuildPreview;
            }

            if (previewStartButton_view != null)
            {
                previewStartButton_view.OnPushButtonListner += StartFullScreenPreview;
            }

            if (backEditorButton_view != null)
            {
                backEditorButton_view.OnPushButtonListner += BackToEditor;
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

        /// <summary>
        /// 全画面プレビューから、編集画面へ戻る (Back Editor ボタンと、メニューの「編集画面へ戻る」から呼ばれる)
        /// </summary>
        public void BackToEditor()
        {
            if (dataGetter_model.EditNoteType.Value != EditNoteType.Preview) { return; }

            ChangeEditMode(EditMode.None);
            ChangeEditNoteType(GetReturnLayer());
        }

        /// <summary>
        /// 全画面プレビューを開く (Preview ボタンと、メニューの「全画面プレビュー」から呼ばれる)
        /// </summary>
        public void StartFullScreenPreview()
        {
            // すでに全画面プレビューの中にいるときは、何もしない (戻る先のレイヤーを、プレビューで上書きしないように)
            if (dataGetter_model.EditNoteType.Value == EditNoteType.Preview) { return; }

            RunStartPreviewFlowAsync().Forget();
        }

        /// <summary>
        /// プレビューのノーツをすべて作り直す (Update ボタンと、メニューの「プレビューを作り直す」から呼ばれる)
        /// </summary>
        public void RebuildPreview()
        {
            previewRefreshable?.RebuildPreviewFromEditorData();
        }

        private async UniTaskVoid RunStartPreviewFlowAsync()
        {
            editNoteTypeCache = dataGetter_model.EditNoteType.Value;

            // 編集画面で選んでいたノーツの選択は、全画面プレビューへ持ち込まない
            // (全画面プレビューでは、グラウンドのノーツが選択中かどうかが見た目で分からない。
            //  選択が残っていると、何も選んでいないつもりで Delete を押したときに、見えていない選択が消えてしまう)
            notesSetter_model?.ClearSelectingNotes();

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

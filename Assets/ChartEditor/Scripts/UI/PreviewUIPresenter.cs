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

        IChartPreviewRefreshable previewRefreshable;
        EditNoteType editNoteTypeCache = EditNoteType.Ground;

        [Inject]
        public void Construct(
            IChartEditorDataGetter dataGetter,
            IChartEditorDataSetter dataSetter,
            IChartEditorOptionSetter optionDataSetter,
            IChartEditorOptionGetter optionDataGetter,
            INoteSpawnDataOptionSetter noteSpawnDataOptionSetter,
            INoteSpawnDataOptionGetter noteSpawnDataOptionGetter)
        {
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
                previewRefreshButton_view.OnPushButtonListner += () => previewRefreshable?.RefreshPreviewFromEditorDataAsync().Forget();
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
                    ChangeEditNoteType(editNoteTypeCache);
                };
            }

            offsetInputField_view.OnFloatValueChangedListner += dataSetter_model.SetOffset;
            changeLaneDivNumButton_view.OnPushButtonListner += () => optionSetter_model.SetLaneDivisionNum(true);

            noteSpeedSlider_view.OnNoteSpeedApplyListener += (value) =>
            {
                noteSpawnDataOptionSetter_model.SetNoteSpeed(value);
                previewRefreshable?.RefreshPreviewFromEditorDataAsync().Forget();
            };
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

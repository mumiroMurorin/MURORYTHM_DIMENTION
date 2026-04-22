using ChartConvert;
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
        [SerializeField] SliderView noteSpeedSlider_view;

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
        public void Construct(IChartEditorDataGetter dataGetter, 
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
            // オフセットの変更
            dataGetter_model?.Offset
                .Subscribe(offsetInputField_view.OnChangeFloatValue)
                .AddTo(this.gameObject);

            // レーン分割数の変更
            optionGetter_model?.LaneDivisionNum
                .Subscribe(changeLaneDivNumButton_view.OnLaneDivNumChanged)
                .AddTo(this.gameObject);

            // ノートスピードの変更
            noteSpawnDataOptionGetter_model?.NoteSpeed
                .Subscribe(noteSpeedSlider_view.OnValueChanged)
                .AddTo(this.gameObject);
        }

        private void SetEvent()
        {
            if (previewRefreshButton_view != null)
            {
                previewRefreshButton_view.OnPushButtonListner += RefreshPreview;
            }

            if (previewStartButton_view != null)
            {
                previewStartButton_view.OnPushButtonListner += () =>
                {
                    editNoteTypeCache = dataGetter_model.EditNoteType.Value;

                    RefreshPreview();
                    ChangeEditMode(EditMode.Preview);
                    ChangeEditNoteType(EditNoteType.Preview);
                };
            }

            if (backEditorButton_view != null)
            {
                backEditorButton_view.OnPushButtonListner += () => { 
                    ChangeEditMode(EditMode.None);
                    ChangeEditNoteType(editNoteTypeCache);
                };
            }

            // オフセットフィールド
            offsetInputField_view.OnFloatValueChangedListner += dataSetter_model.SetOffset;

            // レーン分割数
            changeLaneDivNumButton_view.OnPushButtonListner += () => optionSetter_model.SetLaneDivisionNum(true);

            // ノートスピード
            noteSpeedSlider_view.OnSliderChangedListener += (value) => noteSpawnDataOptionSetter_model.SetNoteSpeed(value);
        }

        private void RefreshPreview()
        {
            if (previewRefreshable == null) { return; }
            if (dataGetter_model?.ChartData?.Value == null) { return; }

            ChartExporter exporter = new ChartExporter();
            ChartDataOrigin chartDataOrigin = exporter.Export(dataGetter_model.ChartData.Value, dataGetter_model.Offset.Value);
            previewRefreshable.RefreshPreview(chartDataOrigin, ChartFilePathCache.CurrentChartFilePath);
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

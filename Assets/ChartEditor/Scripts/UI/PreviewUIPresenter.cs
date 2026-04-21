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

        [Space(20)]
        [Header("Models")]
        [SerializeField] MonoBehaviour previewRefreshTarget;

        IChartEditorDataGetter dataGetter_model;
        IChartEditorDataSetter dataSetter_model;
        IChartPreviewRefreshable previewRefreshable;
        EditNoteType editNoteTypeCache = EditNoteType.Ground;

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter dataSetter)
        {
            this.dataGetter_model = dataGetter;
            this.dataSetter_model = dataSetter;
        }

        private void Start()
        {
            previewRefreshable = previewRefreshTarget as IChartPreviewRefreshable;

            Bind();
            SetEvent();
        }

        private void Bind()
        {

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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

namespace ChartEditor
{
    public class PreviewController : MonoBehaviour
    {
        [SerializeField] GameObject previewCameraParent;

        IChartEditorDataGetter chartEditorDataGetter;
        IChartEditorOptionGetter optionGetter;

        [Inject]
        public void Construct(IChartEditorDataGetter chartEditorDataGetter, IChartEditorOptionGetter optionGetter)
        {
            this.chartEditorDataGetter = chartEditorDataGetter;
            this.optionGetter = optionGetter;
        }

        private void Start()
        {
            Bind();
        }

        private void Bind()
        {
            // エディタノーツモード変更の際カメラオンオフ切り替え
            chartEditorDataGetter?.EditNoteType
                .Subscribe(mode => {
                    previewCameraParent.SetActive(mode == EditNoteType.Preview);
                })
                .AddTo(this.gameObject);
        }
    }

}
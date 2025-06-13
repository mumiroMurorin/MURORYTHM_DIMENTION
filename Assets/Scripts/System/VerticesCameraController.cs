using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

namespace ChartEditor
{
    public class VerticesCameraController : MonoBehaviour
    {
        [SerializeField] GameObject viewCameraParent;

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
                    if (mode == EditNoteType.Ground) { viewCameraParent.SetActive(false); }
                    else if (mode == EditNoteType.Space) { viewCameraParent.SetActive(false); }
                    else if (mode == EditNoteType.Vertices) { viewCameraParent.SetActive(true); }
                })
                .AddTo(this.gameObject);
        }
    }

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

namespace ChartEditor
{
    public class PreviewCameraController : MonoBehaviour
    {
        [SerializeField] GameObject previewCameraParent;

        /// <summary>
        /// 全画面プレビューを映しているカメラ (全画面プレビューの間だけ有効になる)
        /// </summary>
        public Camera ScreenCamera => previewCameraParent != null ? previewCameraParent.GetComponentInChildren<Camera>(true) : null;

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
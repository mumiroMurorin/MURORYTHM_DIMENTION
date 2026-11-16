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

        [Tooltip("全画面プレビューの間だけ動かす、画面に直接映すカメラ (ノーツ・エフェクトを手前に重ねて描くカメラ)。\n" +
                 "空のときは、previewCameraParent と同じ親の下にある、画面に直接映すカメラのうち、ほかのカメラの上に重ねて描くもの (深度 0 以上) を自動で探す")]
        [SerializeField] Camera[] overlayPreviewCameras;

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
            if (overlayPreviewCameras == null || overlayPreviewCameras.Length == 0) { overlayPreviewCameras = FindOverlayCameras(); }
            Bind();
        }

        private void Bind()
        {
            // エディタノーツモード変更の際カメラオンオフ切り替え
            chartEditorDataGetter?.EditNoteType
                .Subscribe(mode => {
                    bool isPreview = mode == EditNoteType.Preview;
                    previewCameraParent.SetActive(isPreview);

                    // ノーツ・エフェクトを手前に重ねて描くカメラも、全画面プレビューの間だけ動かす。
                    // 編集画面のカメラと同じ深度 (0) のものがあり、描く順番は決まっていない。
                    // 動かしたままだと、全画面プレビューから戻ったときなどに、編集画面 (頂点レイヤーなど) の上に、プレビューのノーツが描かれてしまう
                    foreach (var overlayCamera in overlayPreviewCameras)
                    {
                        if (overlayCamera != null && overlayCamera.enabled != isPreview) { overlayCamera.enabled = isPreview; }
                    }
                })
                .AddTo(this.gameObject);
        }

        /// <summary>
        /// 全画面プレビューのカメラと同じ親の下にある、画面に直接映して、ほかのカメラの上に重ねて描くカメラ
        /// (テクスチャに映すカメラ (ミニプレビュー) と、いちばん奥に描くカメラ (深度が負) は除く)
        /// </summary>
        private Camera[] FindOverlayCameras()
        {
            var result = new List<Camera>();
            Transform root = previewCameraParent != null ? previewCameraParent.transform.parent : null;
            if (root == null) { return result.ToArray(); }

            foreach (var candidate in root.GetComponentsInChildren<Camera>(true))
            {
                if (candidate == null) { continue; }
                if (candidate.transform.IsChildOf(previewCameraParent.transform)) { continue; }
                if (candidate.targetTexture != null) { continue; }
                if (candidate.depth < 0f) { continue; }

                result.Add(candidate);
            }

            return result.ToArray();
        }
    }
}

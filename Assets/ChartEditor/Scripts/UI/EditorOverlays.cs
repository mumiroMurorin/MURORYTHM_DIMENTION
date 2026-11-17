using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 画面全体に重ねる部品 (シーンに置いてある。前は実行時に作っていた)
    ///
    /// ・LoadingBlockerCanvas (重なり順 30000、クリックを受け取る): 譜面の読み込み中に、画面全体を薄く覆ってクリックを止める (LoadingInputBlocker)
    /// ・PreviewRangeRectCanvas (20001、クリックを受け取らない): プレビューの上の範囲選択の枠 (PreviewRangeRect)
    /// ・PreviewVertexHandlesCanvas (20000、クリックを受け取らない): プレビューの頂点のつまみの土台 (PreviewVertexHandles。つまみは数が変わるので、実行時に作る)
    /// </summary>
    public class EditorOverlays : MonoBehaviour
    {
        [Tooltip("読み込み中の覆い (読み込んでいる間だけ出す)")]
        [SerializeField] GameObject loadingBlocker;

        [Tooltip("範囲選択の枠の土台 (範囲選択をしている間だけ出す)")]
        [SerializeField] GameObject rangeRect;
        [SerializeField] RectTransform rangeFill;
        [Tooltip("範囲選択の枠のふち (下・上・左・右)")]
        [SerializeField] RectTransform[] rangeBorders = new RectTransform[4];

        [Tooltip("頂点のつまみを置く土台")]
        [SerializeField] RectTransform vertexHandleCanvas;

        static EditorOverlays instance;

        /// <summary>
        /// シーンに置いてある部品 (無ければ null。読み込み中の覆いなどは、最初に使うときに探す)
        /// </summary>
        public static EditorOverlays Instance
        {
            get
            {
                if (instance == null) { instance = FindObjectOfType<EditorOverlays>(true); }
                return instance;
            }
        }

        public GameObject LoadingBlocker => loadingBlocker;
        public GameObject RangeRect => rangeRect;
        public RectTransform RangeFill => rangeFill;
        public RectTransform[] RangeBorders => rangeBorders;
        public RectTransform VertexHandleCanvas => vertexHandleCanvas;

        private void OnDestroy()
        {
            if (instance == this) { instance = null; }
        }
    }
}

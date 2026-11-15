using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace ChartEditor
{
    /// <summary>
    /// 全画面プレビューの、頂点の道具の列
    ///
    /// 編集画面の頂点レイヤーの道具の列 (編集・回転・拡大縮小のボタン、頂点スライド、反転) を、そのまま複製したもの。
    /// シーンの PreviewCanvas に置いてあり、頂点を持つノーツを1つだけ選んでいる間だけ出す (EditorMenuPresenter が出し入れする)。
    /// ここは、ボタンが押されたことを知らせるのと、見た目を今の状態に合わせるのだけを行う。
    ///
    /// 頂点レイヤーの列と違うところ: 選んでいるツールのボタンも、押せるままにしてある (もう一度押すと、選ぶのをやめる)
    /// (今の状態でできる操作は、画面の右側の操作説明の欄に出る)
    /// </summary>
    public class PreviewVertexToolsView : MonoBehaviour
    {
        [Serializable]
        class ToolButton
        {
            public ChangeEditModeButtonView view;
            public PreviewVertexTool tool;
        }

        [Tooltip("道具の列 (出したり隠したりする)")]
        [SerializeField] GameObject list;
        [SerializeField] ToolButton[] toolButtons;
        [SerializeField] VertexIndicesSliderButtonView slideClockwiseButton;
        [SerializeField] VertexIndicesSliderButtonView slideCounterclockwiseButton;
        [Tooltip("横の軸で折り返す (上下を入れ替える)")]
        [SerializeField] VerticesMirrorButtonView mirrorXAxisButton;
        [Tooltip("縦の軸で折り返す (左右を入れ替える)")]
        [SerializeField] VerticesMirrorButtonView mirrorYAxisButton;

        /// <summary>ツールのボタンが押された</summary>
        public event Action<PreviewVertexTool> OnToolClickedListener;
        /// <summary>頂点スライドのボタンが押された (頂点の番号をずらす数。頂点レイヤーのボタンと同じ向き)</summary>
        public event Action<int> OnSlideClickedListener;
        /// <summary>反転のボタンが押された (true = 左右を入れ替える、false = 上下を入れ替える)</summary>
        public event Action<bool> OnMirrorClickedListener;

        bool hasShownTool;
        PreviewVertexTool shownTool;

        void Awake()
        {
            if (toolButtons != null)
            {
                foreach (var button in toolButtons)
                {
                    if (button == null || button.view == null) { continue; }

                    PreviewVertexTool tool = button.tool;
                    button.view.OnClickedListner += () =>
                    {
                        // ボタンが「選択中」のまま残らないようにする
                        // (選んでいるツールのボタンも押せるままなので、残っていると、Space や Enter でもう一度押されて、ツールが外れる)
                        if (EventSystem.current != null) { EventSystem.current.SetSelectedGameObject(null); }
                        OnToolClickedListener?.Invoke(tool);
                    };
                }
            }

            // (向きは、頂点レイヤーのボタンと同じ: 時計回り = -1)
            if (slideClockwiseButton != null) { slideClockwiseButton.OnClickedListner += () => OnSlideClickedListener?.Invoke(-1); }
            if (slideCounterclockwiseButton != null) { slideCounterclockwiseButton.OnClickedListner += () => OnSlideClickedListener?.Invoke(1); }
            if (mirrorXAxisButton != null) { mirrorXAxisButton.OnClickedListner += () => OnMirrorClickedListener?.Invoke(false); }
            if (mirrorYAxisButton != null) { mirrorYAxisButton.OnClickedListner += () => OnMirrorClickedListener?.Invoke(true); }
        }

        /// <summary>
        /// 列を出す / 隠す
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            if (list != null && list.activeSelf != isVisible) { list.SetActive(isVisible); }
        }

        /// <summary>
        /// 選んでいるツールのボタンを、選んでいる見た目にする
        /// </summary>
        public void SetTool(PreviewVertexTool tool)
        {
            if (hasShownTool && shownTool == tool) { return; }
            hasShownTool = true;
            shownTool = tool;

            if (toolButtons != null)
            {
                foreach (var button in toolButtons)
                {
                    if (button != null && button.view != null) { button.view.SetChosenLook(button.tool == tool); }
                }
            }
        }
    }
}

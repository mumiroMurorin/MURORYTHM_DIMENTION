using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// SpaceHold の3D編集で、3Dの画面の手前に出す画面部品 (シーンに置いてある。SpaceHold3DEditor が出し入れする)
    ///
    /// ・左上: 道具の列。頂点レイヤーの道具の列と同じ見た目で、2通りある
    ///   - 節を1つだけ選んでいるとき: 頂点の道具 (編集・回転・拡大縮小・頂点スライド・反転。全画面プレビューの列と同じ)
    ///   - それ以外 (選んでいない・2つ以上選んでいる): 節の道具 (節を足す・頂点スライド・反転)
    /// ・右下: ミニプレビュー (編集画面のものを、3Dの画面の手前に出す)。その左に「頂点レイヤーで編集」と「編集画面へ戻る」
    /// ・◆ (制御点) の位置を数値で決める小さい窓 (X・Y・Z)
    ///   数値は、入力を終えたとき (Enter、ほかの場所をクリック) に反映する (NumberInputField)
    /// </summary>
    // ほかの場所のクリックで入力を終えたとき、そのクリックが処理される前に、打った値を反映するため、先に動かす
    [DefaultExecutionOrder(-1000)]
    public class SpaceHold3DOverlayView : MonoBehaviour
    {
        [Serializable]
        class ToolButton
        {
            public ChangeEditModeButtonView view;
            public PreviewVertexTool tool;
        }

        [Tooltip("3D編集を開いている間だけ出す部品 (ぜんぶ、この中にある)")]
        [SerializeField] GameObject content;

        [Header("左上: 節の道具 (節を選んでいない・2つ以上選んでいるとき)")]
        [SerializeField] GameObject sectionList;
        [SerializeField] ChangeEditModeButtonView addSectionButton;
        [SerializeField] VertexIndicesSliderButtonView sectionSlideClockwiseButton;
        [SerializeField] VertexIndicesSliderButtonView sectionSlideCounterclockwiseButton;
        [SerializeField] VerticesMirrorButtonView sectionMirrorXAxisButton;
        [SerializeField] VerticesMirrorButtonView sectionMirrorYAxisButton;

        [Header("左上: 頂点の道具 (節を1つだけ選んでいるとき)")]
        [SerializeField] GameObject vertexList;
        [SerializeField] ToolButton[] toolButtons;
        [SerializeField] VertexIndicesSliderButtonView vertexSlideClockwiseButton;
        [SerializeField] VertexIndicesSliderButtonView vertexSlideCounterclockwiseButton;
        [SerializeField] VerticesMirrorButtonView vertexMirrorXAxisButton;
        [SerializeField] VerticesMirrorButtonView vertexMirrorYAxisButton;
        [Tooltip("頂点の編集を終える (頂点の道具の列の下)")]
        [SerializeField] Button endVertexEditButton;

        [Header("右下")]
        [SerializeField] Button vertexLayerButton;
        [SerializeField] Button closeButton;
        [Tooltip("編集画面のミニプレビューの窓のキャンバス (3D編集の間だけ、手前に出す)")]
        [SerializeField] Canvas miniPreviewCanvas;

        [Header("◆ の位置を数値で決める窓")]
        [SerializeField] RectTransform controlPanel;
        [SerializeField] TMP_InputField controlInputX;
        [SerializeField] TMP_InputField controlInputY;
        [SerializeField] TMP_InputField controlInputZ;
        [SerializeField] Button controlCloseButton;
        [Tooltip("◆ の位置の窓の、まっすぐに戻すボタン (入力の下)")]
        [SerializeField] Button controlStraightButton;

        // ミニプレビューを出す重なり順 (3D編集の画面部品 5、この画面部品 6 より手前、メニュー 10 より奥)
        const int MINI_PREVIEW_ORDER = 7;

        /// <summary>節を足す</summary>
        public event Action OnAddSectionClicked;
        /// <summary>頂点スライド (時計回り = -1、反時計回り = +1。頂点レイヤーのボタンと同じ)</summary>
        public event Action<int> OnSlideClicked;
        /// <summary>反転 (true = 左右、false = 上下)</summary>
        public event Action<bool> OnMirrorClicked;
        /// <summary>頂点の道具のボタン</summary>
        public event Action<PreviewVertexTool> OnToolClicked;
        public event Action OnVertexLayerClicked;
        /// <summary>頂点の編集を終える</summary>
        public event Action OnEndVertexEditClicked;
        public event Action OnCloseClicked;
        /// <summary>◆ の位置の数値が入力されたとき (0 = X、1 = Y、2 = Z、と、その値)</summary>
        public event Action<int, float> OnControlValueEntered;
        public event Action OnControlPanelCloseClicked;
        /// <summary>
        /// 「頂点レイヤーで編集」のボタンを、出す / 隠す (節を1つだけ選んでいる間だけ出す)
        /// </summary>
        public void SetVertexLayerButtonVisible(bool isVisible)
        {
            if (vertexLayerButton != null && vertexLayerButton.gameObject.activeSelf != isVisible) { vertexLayerButton.gameObject.SetActive(isVisible); }
        }

        /// <summary>◆ の位置の窓の「まっすぐに戻す」</summary>
        public event Action OnControlStraightClicked;

        NumberInputField inputX;
        NumberInputField inputY;
        NumberInputField inputZ;

        bool hasShownTool;
        PreviewVertexTool shownTool;

        /// <summary>◆ の位置の窓を出しているか</summary>
        public bool IsControlPanelShown => controlPanel != null && controlPanel.gameObject.activeSelf;

        void Awake()
        {
            inputX = new NumberInputField(controlInputX, value => OnControlValueEntered?.Invoke(0, value));
            inputY = new NumberInputField(controlInputY, value => OnControlValueEntered?.Invoke(1, value));
            inputZ = new NumberInputField(controlInputZ, value => OnControlValueEntered?.Invoke(2, value));

            if (addSectionButton != null) { addSectionButton.OnClickedListner += () => { ReleaseSelection(); OnAddSectionClicked?.Invoke(); }; }

            // (向きは、頂点レイヤーのボタンと同じ: 時計回り = -1)
            ConnectSlide(sectionSlideClockwiseButton, -1);
            ConnectSlide(sectionSlideCounterclockwiseButton, 1);
            ConnectSlide(vertexSlideClockwiseButton, -1);
            ConnectSlide(vertexSlideCounterclockwiseButton, 1);
            ConnectMirror(sectionMirrorXAxisButton, false);
            ConnectMirror(sectionMirrorYAxisButton, true);
            ConnectMirror(vertexMirrorXAxisButton, false);
            ConnectMirror(vertexMirrorYAxisButton, true);

            if (toolButtons != null)
            {
                foreach (var button in toolButtons)
                {
                    if (button == null || button.view == null) { continue; }

                    PreviewVertexTool tool = button.tool;
                    button.view.OnClickedListner += () => { ReleaseSelection(); OnToolClicked?.Invoke(tool); };
                }
            }

            if (vertexLayerButton != null) { vertexLayerButton.onClick.AddListener(() => { ReleaseSelection(); OnVertexLayerClicked?.Invoke(); }); }
            if (endVertexEditButton != null) { endVertexEditButton.onClick.AddListener(() => { ReleaseSelection(); OnEndVertexEditClicked?.Invoke(); }); }
            if (closeButton != null) { closeButton.onClick.AddListener(() => { ReleaseSelection(); OnCloseClicked?.Invoke(); }); }
            if (controlCloseButton != null) { controlCloseButton.onClick.AddListener(() => { ReleaseSelection(); OnControlPanelCloseClicked?.Invoke(); }); }
            if (controlStraightButton != null) { controlStraightButton.onClick.AddListener(() => { ReleaseSelection(); OnControlStraightClicked?.Invoke(); }); }
        }

        void ConnectSlide(VertexIndicesSliderButtonView button, int delta)
        {
            if (button != null) { button.OnClickedListner += () => OnSlideClicked?.Invoke(delta); }
        }

        void ConnectMirror(VerticesMirrorButtonView button, bool isLeftRight)
        {
            if (button != null) { button.OnClickedListner += () => OnMirrorClicked?.Invoke(isLeftRight); }
        }

        void Update()
        {
            // ほかの場所がクリックされたら、そのクリックが処理される前に、打っている値を反映する
            inputX?.CommitIfClickedElsewhere();
            inputY?.CommitIfClickedElsewhere();
            inputZ?.CommitIfClickedElsewhere();
        }

        /// <summary>
        /// 3D編集の画面部品を出す / 隠す (ミニプレビューを、3Dの画面の手前に出す / 元に戻す)
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            if (!isVisible) { HideControlPanel(); }
            if (content != null && content.activeSelf != isVisible) { content.SetActive(isVisible); }

            if (miniPreviewCanvas != null)
            {
                miniPreviewCanvas.overrideSorting = isVisible;
                if (isVisible) { miniPreviewCanvas.sortingOrder = MINI_PREVIEW_ORDER; }
            }
        }

        /// <summary>
        /// 左上の道具の列を切り替える (true = 頂点の道具、false = 節の道具)
        /// </summary>
        public void SetListMode(bool isVertexTools)
        {
            if (vertexList != null && vertexList.activeSelf != isVertexTools) { vertexList.SetActive(isVertexTools); }
            if (sectionList != null && sectionList.activeSelf == isVertexTools) { sectionList.SetActive(!isVertexTools); }
        }

        /// <summary>
        /// 選んでいる頂点の道具のボタンを、選んでいる見た目にする
        /// </summary>
        public void SetTool(PreviewVertexTool tool)
        {
            if (hasShownTool && tool == shownTool) { return; }
            hasShownTool = true;
            shownTool = tool;

            if (toolButtons == null) { return; }
            foreach (var button in toolButtons)
            {
                if (button != null && button.view != null) { button.view.SetChosenLook(button.tool == tool); }
            }
        }

        /// <summary>
        /// ◆ の位置の窓を出す (位置は、画面部品の単位で、左下からの位置。窓の左の端の、高さのまんなかを合わせる)
        /// </summary>
        public void ShowControlPanel(Vector2 position)
        {
            if (controlPanel == null) { return; }

            if (!controlPanel.gameObject.activeSelf) { controlPanel.gameObject.SetActive(true); }
            MoveControlPanel(position);
        }

        /// <summary>
        /// ◆ の位置の窓を動かす (画面からはみ出さないように寄せる)
        /// </summary>
        public void MoveControlPanel(Vector2 position)
        {
            if (controlPanel == null) { return; }

            var canvasRect = controlPanel.parent as RectTransform;
            Vector2 area = canvasRect != null ? canvasRect.rect.size : new Vector2(1920f, 1080f);
            Vector2 size = controlPanel.rect.size;
            const float MARGIN = 8f;
            // 右にはみ出すときは、◆ の左側に出す
            float x = position.x + size.x + MARGIN > area.x ? position.x - size.x - 2f * 24f : position.x;
            x = Mathf.Clamp(x, MARGIN, Mathf.Max(MARGIN, area.x - size.x - MARGIN));
            float y = Mathf.Clamp(position.y, size.y * 0.5f + MARGIN, Mathf.Max(size.y * 0.5f + MARGIN, area.y - size.y * 0.5f - MARGIN - 34f));
            controlPanel.anchoredPosition = new Vector2(x, y);
        }

        public void HideControlPanel()
        {
            if (controlPanel != null && controlPanel.gameObject.activeSelf)
            {
                // 打っている途中で消すときは、そこまでに打った値を反映してから消す (ほかの場所をクリックしたときと同じ)
                CommitTyping();
                ReleaseSelection();
                controlPanel.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// ◆ の位置の表示を変える (打っている最中の欄は、書き換えない)
        /// </summary>
        public void SetControlValues(float x, float y, float z)
        {
            inputX?.SetText(x.ToString("0.###"));
            inputY?.SetText(y.ToString("0.###"));
            inputZ?.SetText(z.ToString("0.##"));
        }

        /// <summary>
        /// 打っている最中の値があれば、今すぐ入力を終えて反映する (3Dの画面がクリックされて、窓を消す前に呼ぶ)
        /// </summary>
        public void CommitTyping()
        {
            CommitTyping(controlInputX);
            CommitTyping(controlInputY);
            CommitTyping(controlInputZ);
        }

        static void CommitTyping(TMP_InputField field)
        {
            if (field != null && field.isFocused) { field.DeactivateInputField(); }
        }

        /// <summary>
        /// ボタンが「選択中」のまま残らないようにする (Space や Enter で、もう一度押されないように)
        /// </summary>
        static void ReleaseSelection()
        {
            if (EventSystem.current != null && !EventSystem.current.alreadySelecting) { EventSystem.current.SetSelectedGameObject(null); }
        }
    }
}

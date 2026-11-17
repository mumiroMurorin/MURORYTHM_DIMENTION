using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 3D編集で、選んでいる節を回す・拡大縮小するときの動かし方 (録音 20261117_180817 / 190722。数はシーンに書いてある: 変えない)
    /// (2 と 3 は、2026-11-17 に選択モードへ移したので、使わない)
    /// </summary>
    public enum SectionTransformRange
    {
        /// <summary>通常: 選んでいる節を、全部同じだけ</summary>
        Single = 0,
        /// <summary>なめらか: 掴んだ節は全部、選んでいるほかの節は、選んでいる範囲の外側の節に近いほど少しずつ (ねじれるように)</summary>
        Smooth = 1,
    }

    /// <summary>
    /// 3D編集で、節をクリックしたときの選び方 (録音 20261117_190401。数はシーンに書いてある: 変えない)
    /// </summary>
    public enum SectionSelectMode
    {
        /// <summary>通常: クリックで1つ、Ctrl+クリックで加える・外す、Shift+クリックで間をまとめて</summary>
        Normal = 0,
        /// <summary>全体: このボタン (Ctrl+A) で、SpaceHold の節を全部選ぶ</summary>
        All = 1,
        /// <summary>Nつおき: クリックした節と、そこから N つおきの節を選ぶ (N は設定メニューで 1〜4: 録音 20261117_205010)</summary>
        Alternate = 2,
    }

    /// <summary>
    /// SpaceHold の3D編集で、3Dの画面の手前に出す画面部品 (シーンに置いてある。SpaceHold3DEditor が出し入れする)
    ///
    /// ・左上: 道具の列。頂点レイヤーの道具の列と同じ見た目
    ///   節の形の道具 (編集・回転・拡大縮小・頂点スライド・反転)。いつも出す (録音 20261117_201649)。選んでいる節すべてに効く
    ///   (「編集」は、道具を選んでいない状態。頂点レイヤーの「編集」と同じ見た目。開いたときは、これを選んでいる: 録音 20261117_204809)
    ///   (録音 20261117_180313: 3D編集では頂点を足したり動かしたりしない。頂点の編集は、頂点レイヤーで)
    ///   (録音 20261117_190118: 何も選んでいないときの道具 (節を足す・頂点スライド・反転) は、なくした)
    /// ・その右: 回転・拡大縮小の動かし方 (通常 / なめらか) と、選択モード (通常 / 全体 / Nつおき)。いつも出す
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

        [Serializable]
        class RangeButton
        {
            public Button button;
            public SectionTransformRange range;
        }

        [Serializable]
        class SelectModeButton
        {
            public Button button;
            public SectionSelectMode mode;
        }

        [Tooltip("3D編集を開いている間だけ出す部品 (ぜんぶ、この中にある)")]
        [SerializeField] GameObject content;

        [Header("左上: 節の形の道具 (いつも出す)")]
        [SerializeField] GameObject vertexList;
        [SerializeField] ToolButton[] toolButtons;
        [SerializeField] VertexIndicesSliderButtonView vertexSlideClockwiseButton;
        [SerializeField] VertexIndicesSliderButtonView vertexSlideCounterclockwiseButton;
        [SerializeField] VerticesMirrorButtonView vertexMirrorXAxisButton;
        [SerializeField] VerticesMirrorButtonView vertexMirrorYAxisButton;
        [Tooltip("節の選択を外す (選択モードの下。録音 20261117_201748)")]
        [SerializeField] Button endVertexEditButton;
        [Tooltip("回転・拡大縮小の動かし方 (道具の列の右。どちらか1つを選ぶ)")]
        [SerializeField] RangeButton[] rangeButtons;
        [Tooltip("選択モード (動かし方の下。どれか1つを選ぶ)")]
        [SerializeField] SelectModeButton[] selectModeButtons;
        [Tooltip("レイヤーのボタン (編集画面のレイヤーのボタンと同じ場所。押すと、3D編集を閉じてスペースのレイヤーに戻る: 録音 20261117_195031)")]
        [SerializeField] Button layerButton;

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
        [Tooltip("窓の見出し・説明・Z の行 (節の中心の位置を出すときは、見出しと説明を替えて、Z の行を隠す。録音 20261118_020916)")]
        [SerializeField] TextMeshProUGUI controlTitle;
        [SerializeField] TextMeshProUGUI controlHint;
        [SerializeField] GameObject controlRowZ;

        // ミニプレビューを出す重なり順 (3D編集の画面部品 5、この画面部品 6 より手前、メニュー 10 より奥)
        const int MINI_PREVIEW_ORDER = 7;

        /// <summary>頂点スライド (時計回り = -1、反時計回り = +1。頂点レイヤーのボタンと同じ)</summary>
        public event Action<int> OnSlideClicked;
        /// <summary>反転 (true = 左右、false = 上下)</summary>
        public event Action<bool> OnMirrorClicked;
        /// <summary>頂点の道具のボタン</summary>
        public event Action<PreviewVertexTool> OnToolClicked;
        public event Action OnVertexLayerClicked;
        /// <summary>節の選択を外す</summary>
        public event Action OnEndVertexEditClicked;
        /// <summary>回転・拡大縮小の動かし方のボタン</summary>
        public event Action<SectionTransformRange> OnRangeClicked;
        /// <summary>選択モードのボタン</summary>
        public event Action<SectionSelectMode> OnSelectModeClicked;
        public event Action OnCloseClicked;
        /// <summary>レイヤーのボタン</summary>
        public event Action OnLayerClicked;
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
        bool hasShownRange;
        SectionTransformRange shownRange;
        bool hasShownSelectMode;
        SectionSelectMode shownSelectMode;
        int shownAlternateInterval;

        // 範囲のボタンの色 (選んでいるものは、メニューの選んでいる項目と同じ青)
        static readonly Color RANGE_NORMAL = new Color(0.3f, 0.3f, 0.34f, 1f);

        static readonly Color RANGE_CHOSEN = new Color(0.22f, 0.46f, 0.86f, 1f);

        /// <summary>◆ の位置の窓を出しているか</summary>
        public bool IsControlPanelShown => controlPanel != null && controlPanel.gameObject.activeSelf;

        void Awake()
        {
            if (layerButton != null) { layerButton.onClick.AddListener(() => { ReleaseSelection(); OnLayerClicked?.Invoke(); }); }
            inputX = new NumberInputField(controlInputX, value => OnControlValueEntered?.Invoke(0, value));
            inputY = new NumberInputField(controlInputY, value => OnControlValueEntered?.Invoke(1, value));
            inputZ = new NumberInputField(controlInputZ, value => OnControlValueEntered?.Invoke(2, value));

            // (向きは、頂点レイヤーのボタンと同じ: 時計回り = -1)
            ConnectSlide(vertexSlideClockwiseButton, -1);
            ConnectSlide(vertexSlideCounterclockwiseButton, 1);
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

            if (rangeButtons != null)
            {
                foreach (var rangeButton in rangeButtons)
                {
                    if (rangeButton == null || rangeButton.button == null) { continue; }

                    SectionTransformRange range = rangeButton.range;
                    rangeButton.button.onClick.AddListener(() => { ReleaseSelection(); OnRangeClicked?.Invoke(range); });
                }
            }

            if (selectModeButtons != null)
            {
                foreach (var modeButton in selectModeButtons)
                {
                    if (modeButton == null || modeButton.button == null) { continue; }

                    SectionSelectMode mode = modeButton.mode;
                    modeButton.button.onClick.AddListener(() => { ReleaseSelection(); OnSelectModeClicked?.Invoke(mode); });

                    // 全体: Ctrl+A でも選べる (ボタンの右下に、ほかのボタンと同じように小さく出す)
                    if (mode == SectionSelectMode.All)
                    {
                        TextMeshProUGUI badge = ShortcutBadge.Create(modeButton.button.transform, modeButton.button.GetComponentInChildren<TextMeshProUGUI>(true));
                        if (badge != null) { badge.text = "Ctrl+A"; }
                    }
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
        /// 節を選んでいるかに合わせる: 道具の列はいつも出す (録音 20261117_201649)。「節の選択を外す」は、選んでいるときだけ出す
        /// </summary>
        public void SetListMode(bool hasSelection)
        {
            if (vertexList != null && !vertexList.activeSelf) { vertexList.SetActive(true); }
            if (endVertexEditButton != null && endVertexEditButton.gameObject.activeSelf != hasSelection) { endVertexEditButton.gameObject.SetActive(hasSelection); }
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
        /// 選んでいる範囲のボタンを、選んでいる見た目 (青) にする
        /// </summary>
        public void SetRange(SectionTransformRange range)
        {
            if (hasShownRange && range == shownRange) { return; }
            hasShownRange = true;
            shownRange = range;

            if (rangeButtons == null) { return; }
            foreach (var rangeButton in rangeButtons)
            {
                if (rangeButton != null) { SetChosenColor(rangeButton.button, rangeButton.range == range); }
            }
        }

        /// <summary>
        /// 選んでいる選択モードのボタンを、選んでいる見た目 (青) にする
        /// </summary>
        public void SetSelectMode(SectionSelectMode mode)
        {
            if (hasShownSelectMode && mode == shownSelectMode) { return; }
            hasShownSelectMode = true;
            shownSelectMode = mode;

            if (selectModeButtons == null) { return; }
            foreach (var modeButton in selectModeButtons)
            {
                if (modeButton != null) { SetChosenColor(modeButton.button, modeButton.mode == mode); }
            }
        }

        /// <summary>
        /// 選択モード「Nつおき」のボタンの文字を、今の N に合わせる (例: 2 なら「2つおき」)
        /// </summary>
        public void SetAlternateInterval(int interval)
        {
            if (interval == shownAlternateInterval) { return; }
            shownAlternateInterval = interval;

            if (selectModeButtons == null) { return; }
            foreach (var modeButton in selectModeButtons)
            {
                if (modeButton == null || modeButton.button == null || modeButton.mode != SectionSelectMode.Alternate) { continue; }

                var label = modeButton.button.GetComponentInChildren<TextMeshProUGUI>(true);
                if (label != null) { label.text = $"{interval}つおき"; }
            }
        }

        /// <summary>
        /// ボタンの色を、選んでいる (青) / 選んでいない にする (範囲・選択モードのボタン)
        /// </summary>
        static void SetChosenColor(Button button, bool isChosen)
        {
            if (button == null) { return; }

            ColorBlock colors = button.colors;
            colors.normalColor = isChosen ? RANGE_CHOSEN : RANGE_NORMAL;
            colors.selectedColor = colors.normalColor;
            button.colors = colors;
        }

        /// <summary>
        /// 道具のボタンの右下に、ショートカットキーを小さく出す (ほかの道具のボタンと同じ見た目)
        /// </summary>
        public void SetToolShortcut(PreviewVertexTool tool, string text)
        {
            if (toolButtons == null) { return; }
            foreach (var button in toolButtons)
            {
                if (button != null && button.view != null && button.tool == tool) { button.view.SetShortcutText(text); }
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
        string controlTitleOfControl;
        string controlHintOfControl;
        Vector2 controlPanelSizeOfControl;
        bool isSectionCenterMode;

        // 節の中心の位置を出すときの、窓の高さ (見出し・X と Y の行・説明だけ。Z の行と、まっすぐに戻すボタンの分を詰める)
        public const float SECTION_CENTER_PANEL_HEIGHT = 96f;

        /// <summary>
        /// 窓を、◆ の位置 (false) と、節の中心の位置 (true: X・Y だけ。録音 20261118_020916) のどちらで使うか
        /// </summary>
        public void SetControlPanelMode(bool isSectionCenter)
        {
            if (controlTitleOfControl == null)
            {
                controlTitleOfControl = controlTitle != null ? controlTitle.text : string.Empty;
                controlHintOfControl = controlHint != null ? controlHint.text : string.Empty;
                controlPanelSizeOfControl = controlPanel != null ? controlPanel.sizeDelta : Vector2.zero;
            }
            isSectionCenterMode = isSectionCenter;

            if (controlTitle != null) { controlTitle.text = isSectionCenter ? "節の中心の位置" : controlTitleOfControl; }
            if (controlHint != null) { controlHint.text = isSectionCenter ? "X: 横、Y: 縦 (節の面の中の位置)" : controlHintOfControl; }
            if (controlRowZ != null && controlRowZ.activeSelf == isSectionCenter) { controlRowZ.SetActive(!isSectionCenter); }
            if (controlStraightButton != null && controlStraightButton.gameObject.activeSelf == isSectionCenter) { controlStraightButton.gameObject.SetActive(!isSectionCenter); }
            if (controlPanel != null && controlPanelSizeOfControl != Vector2.zero)
            {
                controlPanel.sizeDelta = isSectionCenter ? new Vector2(controlPanelSizeOfControl.x, SECTION_CENTER_PANEL_HEIGHT) : controlPanelSizeOfControl;
            }
        }

        /// <summary>窓を、節の中心の位置で使っているか</summary>
        public bool IsSectionCenterMode => isSectionCenterMode;

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

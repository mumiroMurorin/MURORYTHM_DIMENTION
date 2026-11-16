using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// ホールドの曲がりの制御点 (ひし形の印) の位置を、数値で決める小さい窓 (録音 20261116_233325)
    ///
    /// 印をダブルクリックか右クリックすると、印の横に出る (HoldCurveEditor が出し入れする)。
    /// ・横: 2つの節をまっすぐ結んだ線から、どれだけ横へずらすか (レーンの数。0 でまっすぐ)
    /// ・時間: 2つの節の間のどこか (0 = 前の節、1 = 次の節)
    /// 数値は、入力を終えたとき (Enter、ほかの場所をクリック) に反映する (NumberInputField)。
    /// 入力の下の「まっすぐに戻す」で、2つの節の間をまっすぐに戻す (録音 20261117_005036)。
    /// 画面部品はシーンに置いてある (いつも手前に出すキャンバス)。
    /// </summary>
    // ほかの場所のクリックで入力を終えたとき、そのクリックが処理される前に、打った値を反映するため、先に動かす
    [DefaultExecutionOrder(-1000)]
    public class HoldCurveControlPanelView : MonoBehaviour
    {
        [SerializeField] RectTransform panel;
        [SerializeField] TMP_InputField inputSide;
        [SerializeField] TMP_InputField inputTime;
        [SerializeField] Button closeButton;
        [SerializeField] Button straightButton;

        /// <summary>数値が入力されたとき (0 = 横、1 = 時間、と、その値)</summary>
        public event Action<int, float> OnValueEntered;
        public event Action OnCloseClicked;
        /// <summary>「まっすぐに戻す」が押されたとき</summary>
        public event Action OnStraightClicked;

        NumberInputField side;
        NumberInputField time;

        public bool IsShown => panel != null && panel.gameObject.activeSelf;

        void Awake()
        {
            side = new NumberInputField(inputSide, value => OnValueEntered?.Invoke(0, value));
            time = new NumberInputField(inputTime, value => OnValueEntered?.Invoke(1, value));
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(() =>
                {
                    if (EventSystem.current != null && !EventSystem.current.alreadySelecting) { EventSystem.current.SetSelectedGameObject(null); }
                    OnCloseClicked?.Invoke();
                });
            }
            if (straightButton != null)
            {
                straightButton.onClick.AddListener(() =>
                {
                    if (EventSystem.current != null && !EventSystem.current.alreadySelecting) { EventSystem.current.SetSelectedGameObject(null); }
                    OnStraightClicked?.Invoke();
                });
            }
        }

        void Update()
        {
            side?.CommitIfClickedElsewhere();
            time?.CommitIfClickedElsewhere();
        }

        /// <summary>
        /// 窓を出す / 動かす (位置は画面のピクセル。窓の左の端の、高さのまんなかを合わせる。画面からはみ出さないように寄せる)
        /// </summary>
        public void Show(Vector2 screenPosition)
        {
            if (panel == null) { return; }
            if (!panel.gameObject.activeSelf) { panel.gameObject.SetActive(true); }
            Move(screenPosition);
        }

        public void Move(Vector2 screenPosition)
        {
            if (panel == null) { return; }

            var canvasRect = panel.parent as RectTransform;
            Canvas canvas = panel.GetComponentInParent<Canvas>();
            float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;
            Vector2 area = canvasRect != null ? canvasRect.rect.size : new Vector2(1920f, 1080f);
            Vector2 size = panel.rect.size;
            Vector2 position = screenPosition / scale;
            const float MARGIN = 8f;

            // 右にはみ出すときは、印の左側に出す
            float x = position.x + size.x + MARGIN > area.x ? position.x - size.x - 48f : position.x;
            x = Mathf.Clamp(x, MARGIN, Mathf.Max(MARGIN, area.x - size.x - MARGIN));
            float y = Mathf.Clamp(position.y, size.y * 0.5f + MARGIN, Mathf.Max(size.y * 0.5f + MARGIN, area.y - size.y * 0.5f - MARGIN - 34f));
            panel.anchoredPosition = new Vector2(x, y);
        }

        /// <summary>
        /// 窓を消す (打っている途中の値は、反映してから消す)
        /// </summary>
        public void Hide()
        {
            if (panel == null || !panel.gameObject.activeSelf) { return; }

            CommitTyping();
            if (EventSystem.current != null && !EventSystem.current.alreadySelecting) { EventSystem.current.SetSelectedGameObject(null); }
            panel.gameObject.SetActive(false);
        }

        /// <summary>
        /// 表示を変える (打っている最中の欄は、書き換えない)
        /// </summary>
        public void SetValues(float sideValue, float timeValue)
        {
            side?.SetText(sideValue.ToString("0.##"));
            time?.SetText(timeValue.ToString("0.##"));
        }

        /// <summary>
        /// 打っている最中の値があれば、今すぐ入力を終えて反映する
        /// </summary>
        public void CommitTyping()
        {
            if (inputSide != null && inputSide.isFocused) { inputSide.DeactivateInputField(); }
            if (inputTime != null && inputTime.isFocused) { inputTime.DeactivateInputField(); }
        }

        /// <summary>
        /// 画面の位置が、窓の上か
        /// </summary>
        public bool IsPointerOver(Vector2 screenPosition)
        {
            if (!IsShown) { return false; }

            Canvas canvas = panel.GetComponentInParent<Canvas>();
            Camera eventCamera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.RectangleContainsScreenPoint(panel, screenPosition, eventCamera);
        }
    }
}

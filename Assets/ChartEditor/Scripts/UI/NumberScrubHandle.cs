using System;
using System.Globalization;
using TMPro;
using UndoRedo;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 数値の入力欄の横の文字 (欄の名前) を押したまま左右にドラッグすると、値が増え減りする (録音 20261118_162040)
    ///
    /// Unity のインスペクタと同じ: 右へ動かすほど大きく、左へ動かすほど小さくなる。Shift を押している間は、ゆっくり動く。
    /// ・欄と、ドラッグで値を動かすことを1組にした部品。数値の欄は NumberInputField.EnableScrub で、
    ///   入力を終えたときに反映する欄 (ダイアログなど) は AttachToField で付ける
    /// ・ドラッグしている間は、その場で値を反映する。押してから離すまでの変化は、1回の「元に戻す」にまとめる
    /// ・整数の欄 (小節数・分割数など) は、整数だけで動く
    /// 欄の名前の文字に、このコンポーネントを足して使う (画面部品は増やさない)。
    /// </summary>
    public sealed class NumberScrubHandle : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler,
        IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        /// <summary>
        /// ドラッグで値をどう動かすか
        /// </summary>
        public struct Settings
        {
            /// <summary>1ピクセル (画面の高さ 1080 のとき) 動かしたときに、値がどれだけ変わるか</summary>
            public float StepPerPixel;
            /// <summary>整数だけにするか</summary>
            public bool IsInteger;
            public float Min;
            public float Max;

            /// <summary>小数の欄</summary>
            public static Settings Float(float stepPerPixel, float min = float.NegativeInfinity, float max = float.PositiveInfinity)
            {
                return new Settings { StepPerPixel = stepPerPixel, IsInteger = false, Min = min, Max = max };
            }

            /// <summary>整数の欄 (pixelsPerStep ピクセル動かすごとに 1 つ変わる)</summary>
            public static Settings Integer(float pixelsPerStep = 8f, int min = int.MinValue, int max = int.MaxValue)
            {
                return new Settings { StepPerPixel = 1f / Mathf.Max(1f, pixelsPerStep), IsInteger = true, Min = min, Max = max };
            }
        }

        // 画面の高さの基準 (この高さのとき、StepPerPixel のとおりに動く)
        const float REFERENCE_SCREEN_HEIGHT = 1080f;
        // Shift を押している間の、動く速さの割合
        const float FINE_FACTOR = 0.1f;

        TMP_InputField field;
        Settings settings;
        Action<float> apply;

        float startValue;
        float lastApplied;
        float travel;
        bool isDragging;
        bool isTransactionOpen;
        bool isHovered;

        static CursorTextureChanger cursorChanger;

        /// <summary>どれかの欄の名前をドラッグしている最中か</summary>
        public static bool IsDraggingAny { get; private set; }

        /// <summary>
        /// 欄の名前の文字に、ドラッグで値を動かす働きを付ける
        /// </summary>
        /// <param name="field">値を出している入力欄 (ドラッグを始めたときの値を、ここから読む)</param>
        /// <param name="label">欄の名前の文字 (ここを押してドラッグする)</param>
        /// <param name="apply">ドラッグで変わった値を反映する処理</param>
        public static NumberScrubHandle Attach(TMP_InputField field, Graphic label, Settings settings, Action<float> apply)
        {
            if (field == null || label == null || apply == null) { return null; }

            // (文字が押されたことを受け取れるように)
            label.raycastTarget = true;
            if (!label.TryGetComponent(out NumberScrubHandle handle)) { handle = label.gameObject.AddComponent<NumberScrubHandle>(); }
            handle.field = field;
            handle.settings = settings;
            handle.apply = apply;
            return handle;
        }

        /// <summary>
        /// 入力を終えたときに反映する欄 (onEndEdit で受け取る欄、ダイアログの欄) に付ける
        /// </summary>
        /// <param name="notifiesEndEdit">値が変わるたびに onEndEdit も呼ぶか (false: 欄の文字だけ変える。決定ボタンで読む欄)</param>
        /// <param name="label">欄の名前の文字 (null のときは、欄のすぐ前にある文字)</param>
        public static NumberScrubHandle AttachToField(TMP_InputField field, Settings settings, bool notifiesEndEdit, Graphic label = null)
        {
            if (field == null) { return null; }

            label ??= FindLabel(field);
            return Attach(field, label, settings, value =>
            {
                string text = Format(value, settings.IsInteger);
                field.text = text;
                if (notifiesEndEdit) { field.onEndEdit.Invoke(text); }
            });
        }

        /// <summary>
        /// 欄の名前の文字を探す (同じ親の中で、欄のすぐ前にある文字。間にほかの欄があるときは、無し)
        /// </summary>
        public static Graphic FindLabel(TMP_InputField field)
        {
            if (field == null) { return null; }

            Transform self = field.transform;
            Transform parent = self.parent;
            if (parent == null) { return null; }

            for (int i = self.GetSiblingIndex() - 1; i >= 0; i--)
            {
                Transform sibling = parent.GetChild(i);
                if (!sibling.gameObject.activeSelf) { continue; }
                if (sibling.GetComponent<TMP_InputField>() != null) { return null; }
                if (sibling.TryGetComponent(out TMP_Text text)) { return text; }
            }
            return null;
        }

        /// <summary>
        /// 値を、欄に出す文字にする (整数はそのまま、小数は 4 桁まで)
        /// </summary>
        public static string Format(float value, bool isInteger)
        {
            return isInteger
                ? Mathf.RoundToInt(value).ToString(CultureInfo.InvariantCulture)
                : value.ToString("0.####", CultureInfo.InvariantCulture);
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            // 押した所から、すぐに動かせるように (少し動かすまで待たない)
            eventData.useDragThreshold = false;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) { return; }
            if (field == null || apply == null || !field.interactable || !field.gameObject.activeInHierarchy) { return; }

            // 欄に打っている途中なら、打った値を先に確定する
            if (field.isFocused) { field.DeactivateInputField(); }
            if (!NumberInputField.TryParseNumber(field.text, out startValue)) { return; }
            if (settings.IsInteger) { startValue = Mathf.Round(startValue); }

            isDragging = true;
            IsDraggingAny = true;
            travel = 0f;
            lastApplied = startValue;

            // 押してから離すまでに反映した値は、1回の「元に戻す」にまとめる
            History.BeginTransaction();
            isTransactionOpen = true;
            SetCursor(true);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!isDragging || eventData.button != PointerEventData.InputButton.Left) { return; }

            float screenScale = Mathf.Max(0.01f, Screen.height / REFERENCE_SCREEN_HEIGHT);
            bool isFine = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            travel += eventData.delta.x / screenScale * (isFine ? FINE_FACTOR : 1f);

            // 動かした量を、1ピクセルぶんの刻みにそろえる (整数の欄は、整数に)
            float change = settings.IsInteger
                ? Mathf.Round(travel * settings.StepPerPixel)
                : Mathf.Round(travel) * settings.StepPerPixel;
            float value = Mathf.Clamp(startValue + change, settings.Min, settings.Max);
            if (!settings.IsInteger) { value = (float)Math.Round(value, 4); }

            float tolerance = settings.IsInteger ? 0.5f : settings.StepPerPixel * 0.25f;
            if (Mathf.Abs(value - lastApplied) < tolerance) { return; }
            lastApplied = value;

            try { apply(value); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            Finish();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            SetCursor(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            if (!isDragging) { SetCursor(false); }
        }

        void OnDisable()
        {
            // 窓を閉じた・隠したときも、まとめを閉じる (開いたままだと、その後の操作がすべて1回にまとまってしまう)
            Finish();
            if (isHovered)
            {
                isHovered = false;
                SetCursor(false);
            }
        }

        private void Finish()
        {
            if (!isDragging) { return; }

            isDragging = false;
            IsDraggingAny = false;
            if (isTransactionOpen)
            {
                isTransactionOpen = false;
                History.EndTransaction();
            }
            SetCursor(isHovered);
        }

        /// <summary>
        /// 欄の名前の上と、ドラッグしている間は、左右の矢印のカーソルにする (ノーツの幅を変えるときと同じ絵)
        /// </summary>
        private static void SetCursor(bool isScrubCursor)
        {
            // (譜面エディタのシーンを開き直したときは、探し直す)
            if (cursorChanger == null) { cursorChanger = FindObjectOfType<CursorTextureChanger>(true); }
            if (cursorChanger == null) { return; }

            cursorChanger.SetPreviewCursor(isScrubCursor ? EditMode.Scale : EditMode.None);
        }
    }
}

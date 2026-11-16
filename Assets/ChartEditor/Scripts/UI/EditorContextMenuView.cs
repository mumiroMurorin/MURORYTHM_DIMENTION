using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChartEditor
{
    /// <summary>
    /// 右クリックのメニュー (カーソルの右に出る、押せる項目の縦の並び)
    ///
    /// 画面部品はシーンに置いてある (いちばん手前のキャンバス)。項目の数だけ、置いてある行を使う (足りない分は出さない)。
    /// ・項目を押す: その操作をして、閉じる
    /// ・メニューの外をクリック (どのボタンでも)、Esc: 何もせずに閉じる (そのクリックは、裏の画面には届かない)
    /// 開いている間と、閉じたフレームは、メニューバーの一覧を開いているときと同じく、裏の譜面の操作を止める
    /// (EditorMenuBar.IsMenuOpenOrJustClosed が、これも見る)。
    /// </summary>
    public class EditorContextMenuView : MonoBehaviour
    {
        /// <summary>メニューの項目1つ</summary>
        public readonly struct Item
        {
            public readonly string Label;
            public readonly Action Action;
            public readonly bool IsEnabled;

            public Item(string label, Action action, bool isEnabled = true)
            {
                Label = label;
                Action = action;
                IsEnabled = isEnabled;
            }
        }

        [Serializable]
        class Row
        {
            public Button button;
            public TextMeshProUGUI label;
        }

        [Tooltip("メニューの外のクリックを受け取る、画面いっぱいの透明な板 (メニューを開いている間だけ出す)")]
        [SerializeField] GameObject blocker;
        [Tooltip("項目を並べる枠")]
        [SerializeField] RectTransform panel;
        [SerializeField] Row[] rows;

        static EditorContextMenuView instance;
        static int lastClosedFrame = -1;

        Action[] actions;
        bool isOpen;
        // 開いたフレーム (このフレームの、外側の板へのクリックは、メニューを開いたクリックそのものなので、受け取らない)
        int openedFrame = -1;

        /// <summary>メニューを開いているか</summary>
        public static bool IsOpen => instance != null && instance.isOpen;

        /// <summary>メニューを開いているか、閉じたフレームか (閉じたクリックで、裏の譜面が操作されないように)</summary>
        public static bool IsOpenOrJustClosed => IsOpen || Time.frameCount == lastClosedFrame;

        void Awake()
        {
            instance = this;
            actions = new Action[rows != null ? rows.Length : 0];

            for (int i = 0; i < actions.Length; i++)
            {
                int index = i;
                if (rows[i] != null && rows[i].button != null) { rows[i].button.onClick.AddListener(() => OnRowClicked(index)); }
            }

            if (blocker != null)
            {
                // メニューの外を押したら閉じる (押した時点で閉じて、そのクリックは裏へ通さない)
                var trigger = blocker.GetComponent<EventTrigger>() ?? blocker.AddComponent<EventTrigger>();
                var entry = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
                entry.callback.AddListener(_ =>
                {
                    // 開いたフレームの押し込みは、メニューを開いた右クリックそのもの (前に出したことがある板は、出したフレームから
                    // クリックを受け取ってしまう)。これで閉じると、2回右クリックしないとメニューが出ないように見える
                    if (Time.frameCount == openedFrame) { return; }
                    Close();
                });
                trigger.triggers.Add(entry);
            }

            SetShown(false);
        }

        void OnDestroy()
        {
            if (instance == this) { instance = null; }
        }

        void Update()
        {
            if (!isOpen) { return; }

            if (Input.GetKeyDown(KeyCode.Escape)) { Close(); }
        }

        /// <summary>
        /// メニューを出す (項目が1つも無いときは、出さない)
        /// </summary>
        /// <param name="screenPosition">カーソルの位置 (画面のピクセル)。メニューは、その右下に出る</param>
        public static bool Show(Vector2 screenPosition, IReadOnlyList<Item> items)
        {
            if (instance == null || items == null || items.Count == 0) { return false; }

            return instance.Open(screenPosition, items);
        }

        /// <summary>
        /// 開いていれば閉じる
        /// </summary>
        public static void Hide()
        {
            if (instance != null) { instance.Close(); }
        }

        private bool Open(Vector2 screenPosition, IReadOnlyList<Item> items)
        {
            if (rows == null || panel == null) { return false; }

            int shown = 0;
            for (int i = 0; i < rows.Length; i++)
            {
                bool isUsed = i < items.Count;
                if (rows[i] == null || rows[i].button == null) { continue; }

                rows[i].button.gameObject.SetActive(isUsed);
                if (!isUsed)
                {
                    actions[i] = null;
                    continue;
                }

                rows[i].label.text = items[i].Label;
                rows[i].button.interactable = items[i].IsEnabled;
                actions[i] = items[i].Action;
                shown++;
            }
            if (shown == 0) { return false; }

            if (items.Count > rows.Length) { Debug.LogWarning($"【右クリックのメニュー】項目が多すぎます ({items.Count} 個、出せるのは {rows.Length} 個まで)"); }

            SetShown(true);
            isOpen = true;
            openedFrame = Time.frameCount;
            Place(screenPosition);
            if (EventSystem.current != null) { EventSystem.current.SetSelectedGameObject(null); }
            return true;
        }

        private void Close()
        {
            if (!isOpen) { return; }

            isOpen = false;
            lastClosedFrame = Time.frameCount;
            SetShown(false);
        }

        private void OnRowClicked(int index)
        {
            Action action = index >= 0 && index < actions.Length ? actions[index] : null;
            Close();
            if (EventSystem.current != null) { EventSystem.current.SetSelectedGameObject(null); }

            try { action?.Invoke(); }
            catch (Exception exception) { Debug.LogException(exception); }
        }

        private void SetShown(bool isShown)
        {
            if (blocker != null && blocker.activeSelf != isShown) { blocker.SetActive(isShown); }
            if (panel != null && panel.gameObject.activeSelf != isShown) { panel.gameObject.SetActive(isShown); }
        }

        /// <summary>
        /// カーソルの右下に置く (画面からはみ出すときは、左や上へ寄せる)
        /// </summary>
        private void Place(Vector2 screenPosition)
        {
            var canvasRect = panel.parent as RectTransform;
            if (canvasRect == null) { return; }

            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            Canvas canvas = panel.GetComponentInParent<Canvas>();
            float scale = canvas != null && canvas.scaleFactor > 0f ? canvas.scaleFactor : 1f;

            Vector2 area = canvasRect.rect.size;
            Vector2 size = panel.rect.size;
            Vector2 position = screenPosition / scale + new Vector2(6f, -4f);

            // パネルの左上を、位置に合わせる (アンカーは左下)
            float x = position.x + size.x > area.x ? position.x - size.x - 12f : position.x;
            float top = position.y;
            if (top - size.y < 0f) { top = size.y; }
            x = Mathf.Clamp(x, 0f, Mathf.Max(0f, area.x - size.x));
            top = Mathf.Clamp(top, size.y, area.y);

            panel.anchoredPosition = new Vector2(x, top);
        }
    }
}

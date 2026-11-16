using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 画面部品を、透明にして隠す (クリックも通さない)。隠す理由ごとに覚えて、理由が1つでもあれば隠す
    ///
    /// 同じ部品を、表示メニュー (EditorPanelSwitcher) と、全画面プレビューの再生中 (PreviewPlaybackUi) の両方が隠すことがある。
    /// それぞれが CanvasGroup を直接いじると、片方が出したときに、もう片方の「隠す」が消えてしまうので、ここでまとめる。
    /// (非表示 (SetActive) にはしない。隠している間も、中の処理 (ログを受け取るなど) を動かし続けるため)
    /// </summary>
    public static class UiHideReasons
    {
        /// <summary>表示メニューで隠した</summary>
        public const string PANEL = "panel";
        /// <summary>全画面プレビューの再生中</summary>
        public const string PLAYBACK = "playback";

        static readonly Dictionary<GameObject, HashSet<string>> reasons = new Dictionary<GameObject, HashSet<string>>();

        /// <summary>
        /// その理由で、隠す / 隠さない
        /// </summary>
        public static void Set(GameObject target, string reason, bool isHidden)
        {
            if (target == null) { return; }

            // 消えた部品の分は、ついでに捨てる
            if (reasons.Count > 0) { RemoveDestroyed(); }

            if (!reasons.TryGetValue(target, out var set))
            {
                // 隠したことが無い部品には、何も足さない
                if (!isHidden) { return; }
                set = new HashSet<string>();
                reasons.Add(target, set);
            }

            if (isHidden) { set.Add(reason); }
            else { set.Remove(reason); }

            Apply(target, set.Count > 0);
        }

        /// <summary>その部品を、今、何かの理由で隠しているか</summary>
        public static bool IsHidden(GameObject target)
        {
            return target != null && reasons.TryGetValue(target, out var set) && set.Count > 0;
        }

        private static void Apply(GameObject target, bool isHidden)
        {
            var group = target.GetComponent<CanvasGroup>();
            if (group == null)
            {
                if (!isHidden) { return; }
                group = target.AddComponent<CanvasGroup>();
            }

            group.alpha = isHidden ? 0f : 1f;
            group.blocksRaycasts = !isHidden;
            group.interactable = !isHidden;
        }

        static readonly List<GameObject> destroyedBuffer = new List<GameObject>();

        private static void RemoveDestroyed()
        {
            foreach (var key in reasons.Keys)
            {
                if (key == null) { destroyedBuffer.Add(key); }
            }
            foreach (var key in destroyedBuffer) { reasons.Remove(key); }
            destroyedBuffer.Clear();
        }
    }
}

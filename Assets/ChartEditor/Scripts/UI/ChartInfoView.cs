using System.Text;
using TMPro;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 編集画面の「譜面情報」の欄 (シーンに置いてある画面部品)
    ///
    /// 閉じているときは見出しとコンボ数だけを小さく出す。
    /// 見出しのあたりをクリックすると下へ開いて、ノーツの種類ごとの数、小節数、譜面の長さ、BPM も出る。
    /// もう一度クリックすると閉じる。
    ///
    /// 位置・大きさ・文字はシーン上で調整できる。欄は「開いたときの姿」で置いておくこと。
    /// (閉じたときの姿は、開いたときの姿から「開いたときにだけ見える部分」の高さを引いて作る)
    /// 表示する値は ChartInfoPresenter から受け取る。
    /// </summary>
    public class ChartInfoView : MonoBehaviour
    {
        [Header("欄")]
        [Tooltip("欄そのもの。シーンには、開いたときの大きさで置いておく")]
        [SerializeField] RectTransform panel;
        [Tooltip("開いたときにだけ見える部分。シーンには、開いたときの高さで置いておく (閉じるときは、高さを0まで縮めて隠す)")]
        [SerializeField] RectTransform details;
        [Tooltip("クリックすると開閉する場所 (見出しとコンボ数のあたり)")]
        [SerializeField] ClickArea header;

        [Header("文字")]
        [Tooltip("見出しの横の印 (閉じているときと開いているときで、文字を切り替える)")]
        [SerializeField] TextMeshProUGUI markerTmp;
        [Tooltip("コンボ数の値")]
        [SerializeField] TextMeshProUGUI comboTmp;
        [Tooltip("開いたときに出る値の列。上から、タッチ、ダイナミック↑↓→←、ホールド、スペースホールド、スペースブレイク、(区切りの行)、小節数、長さ、BPM の順に書き込む。隣の項目名の列も、同じ並びにしておく")]
        [SerializeField] TextMeshProUGUI detailValuesTmp;

        [Header("開閉")]
        [Tooltip("閉じているときの欄の幅 (開いているときの幅は、シーンに置いた欄の幅)")]
        [SerializeField] float collapsedWidth = 150f;
        [Tooltip("開く・閉じるのにかける時間(秒)")]
        [SerializeField] float toggleSeconds = 0.12f;
        [Tooltip("閉じているときの印 (押すと下に開く)")]
        [SerializeField] string closedMark = "▼";
        [Tooltip("開いているときの印 (押すと上に閉じる)")]
        [SerializeField] string openedMark = "▲";

        // 項目の区切りの行 (項目名の列に入れてある区切りの行と、同じ高さにする)
        const string GAP_LINE = "<size=35%> </size>\n";

        bool isInitialized;
        // 開いたときの欄の大きさと、開いたときにだけ見える部分の高さ (シーンに置いてある姿から読み取る)
        Vector2 expandedSize;
        float detailsHeight;
        // 開いているか (最初は閉じていて、コンボ数だけを出す)
        bool isExpanded;
        // 開き具合 (0 = 閉じている、1 = 開いている。開閉の途中は、その間の値)
        float openRatio;

        readonly StringBuilder builder = new StringBuilder();

        /// <summary>
        /// 開いたときに出る部分が、今見えているか (開いている、または閉じる途中)
        /// 見えていない間は、ノーツの種類ごとの数などを数えなくてよい
        /// </summary>
        public bool IsDetailShown => isExpanded || openRatio > 0f;

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// シーンに置いてある姿(開いたときの姿)を覚えてから、閉じた姿にする (最初の1回だけ)
        /// </summary>
        private void Initialize()
        {
            if (isInitialized) { return; }
            isInitialized = true;

            if (panel != null) { expandedSize = panel.sizeDelta; }
            if (details != null) { detailsHeight = details.sizeDelta.y; }
            if (header != null) { header.OnClickedListener = Toggle; }

            ApplyOpenRatio();
            ApplyMarker();
        }

        private void Update()
        {
            float target = isExpanded ? 1f : 0f;
            if (openRatio == target) { return; }

            // 開き具合を、開いている・閉じているの状態へ少しずつ近づける
            openRatio = toggleSeconds > 0f
                ? Mathf.MoveTowards(openRatio, target, Time.unscaledDeltaTime / toggleSeconds)
                : target;

            ApplyOpenRatio();
        }

        /// <summary>
        /// 欄を出す・隠す
        /// </summary>
        public void SetVisible(bool isVisible)
        {
            if (gameObject.activeSelf != isVisible) { gameObject.SetActive(isVisible); }
        }

        /// <summary>
        /// コンボ数を表示する
        /// </summary>
        public void SetCombo(int combo)
        {
            Initialize();
            if (comboTmp != null) { comboTmp.text = combo.ToString(); }
        }

        /// <summary>
        /// ノーツの種類ごとの数、小節数、譜面の長さ、BPM を表示する
        /// </summary>
        /// <param name="seconds">譜面の長さ(秒)</param>
        public void SetDetails(ChartStatisticsData statistics, int seconds)
        {
            Initialize();
            if (detailValuesTmp == null) { return; }

            // 項目名の列と、行の並びを合わせること
            builder.Clear();
            builder.Append(statistics.Touch).Append('\n');
            builder.Append(statistics.DynamicUpward).Append('\n');
            builder.Append(statistics.DynamicDownward).Append('\n');
            builder.Append(statistics.DynamicRightward).Append('\n');
            builder.Append(statistics.DynamicLeftward).Append('\n');
            builder.Append(statistics.Hold).Append('\n');
            builder.Append(statistics.SpaceHold).Append('\n');
            builder.Append(statistics.SpaceBreak).Append('\n');
            builder.Append(GAP_LINE);
            builder.Append(statistics.BarCount).Append('\n');
            builder.Append(seconds / 60).Append(':').Append((seconds % 60).ToString("00")).Append('\n');
            AppendBpm(statistics);

            detailValuesTmp.text = builder.ToString();
        }

        /// <summary>
        /// BPM を書き足す。途中で変わる譜面では「いちばん遅い値〜いちばん速い値」にする
        /// </summary>
        private void AppendBpm(ChartStatisticsData statistics)
        {
            if (statistics.BarCount == 0)
            {
                builder.Append('-');
                return;
            }

            builder.Append(statistics.MinBpm.ToString("0.##"));
            if (statistics.MaxBpm != statistics.MinBpm)
            {
                builder.Append('〜').Append(statistics.MaxBpm.ToString("0.##"));
            }
        }

        /// <summary>
        /// 見出しがクリックされたとき。開いていれば閉じ、閉じていれば開く
        /// </summary>
        private void Toggle()
        {
            isExpanded = !isExpanded;
            ApplyMarker();
        }

        /// <summary>
        /// 見出しの横の印を、開いているかどうかに合わせる
        /// </summary>
        private void ApplyMarker()
        {
            if (markerTmp != null) { markerTmp.text = isExpanded ? openedMark : closedMark; }
        }

        /// <summary>
        /// 欄の大きさを、今の開き具合に合わせる
        /// (開いたときにだけ見える部分は、高さを縮めた分だけ下から切り取られて見えなくなる)
        /// </summary>
        private void ApplyOpenRatio()
        {
            float hiddenHeight = detailsHeight * (1f - openRatio);

            if (details != null)
            {
                details.sizeDelta = new Vector2(details.sizeDelta.x, detailsHeight - hiddenHeight);
            }

            if (panel != null)
            {
                panel.sizeDelta = new Vector2(
                    Mathf.Lerp(Mathf.Min(collapsedWidth, expandedSize.x), expandedSize.x, openRatio),
                    expandedSize.y - hiddenHeight);
            }
        }
    }
}

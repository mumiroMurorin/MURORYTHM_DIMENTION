using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using VContainer.Unity;

namespace ChartEditor
{
    /// <summary>
    /// レギュレーション違反(赤いアウトラインが出ているノーツ)を、画面のログ欄に知らせる
    /// 違反がある間は表示し続け、すべて解消されたら消す
    /// </summary>
    public class RegulationWarningNotifier : ITickable, IDisposable
    {
        // 違反の有無を調べる間隔(秒)
        const float CHECK_INTERVAL_SECONDS = 0.25f;
        // 違反の内容がこの秒数変わらなかったら表示に反映する
        // (ノーツをドラッグで通過させただけの一瞬の重なりで、表示がちらつかないように)
        const float STABLE_SECONDS = 0.4f;
        // 違反の種類ごとに表示する場所の数
        const int MAX_LOCATION_COUNT = 3;

        // 表示する順番と文言
        static readonly (NoteWarningType Type, string Label)[] warningLabels =
        {
            (NoteWarningType.Stacking, "重なっているノーツ"),
            (NoteWarningType.UnconnectedHold, "つながっていないホールド"),
            (NoteWarningType.UnconnectedSpaceHold, "つながっていないスペースホールド"),
        };

        readonly INotesDataGetter notesGetter;

        readonly Dictionary<NoteWarningType, WarningSummary> summaries = new Dictionary<NoteWarningType, WarningSummary>();
        readonly StringBuilder textBuilder = new StringBuilder();

        // 違反が解消されるまでログ欄に出し続けるお知らせ
        readonly LogPanelNotice notice = new LogPanelNotice(LogType.Warning);

        float nextCheckTime;
        string displayedText = string.Empty;
        string pendingText = string.Empty;
        float pendingSinceTime;

        public RegulationWarningNotifier(INotesDataGetter notesGetter)
        {
            this.notesGetter = notesGetter;
        }

        void ITickable.Tick()
        {
            float now = Time.unscaledTime;
            if (now < nextCheckTime) { return; }
            nextCheckTime = now + CHECK_INTERVAL_SECONDS;

            string currentText = BuildWarningText();

            // 表示中の内容と同じ
            if (currentText == displayedText)
            {
                pendingText = currentText;
                // ほかのログが増えても、違反の表示を一番上に保つ
                notice.KeepOnTop();
                return;
            }

            // 内容が変わった直後は、落ち着くまで待つ
            if (currentText != pendingText)
            {
                pendingText = currentText;
                pendingSinceTime = now;
                return;
            }

            if (now - pendingSinceTime < STABLE_SECONDS) { return; }

            Display(currentText);
        }

        void IDisposable.Dispose()
        {
            notice.Hide();
        }

        /// <summary>
        /// 譜面上の全ノーツを調べて、表示する文章を作る。違反が無ければ空文字
        /// </summary>
        private string BuildWarningText()
        {
            foreach (var summary in summaries.Values) { summary.Clear(); }

            int totalCount = 0;
            var dataToNoteObject = notesGetter.DataToNoteObject;
            for (int i = 0; i < dataToNoteObject.Count; i++)
            {
                var pair = dataToNoteObject[i];
                var noteObject = pair.Object;
                if (noteObject == null) { continue; }

                var warnings = noteObject.Warnings;
                if (warnings.Count == 0) { continue; }

                var address = pair.Data.Address;
                for (int j = 0; j < warnings.Count; j++)
                {
                    if (!summaries.TryGetValue(warnings[j], out var summary))
                    {
                        summary = new WarningSummary();
                        summaries.Add(warnings[j], summary);
                    }

                    summary.Add(address.BarIndex, address.SubDivisionIndex);
                    totalCount++;
                }
            }

            if (totalCount == 0) { return string.Empty; }

            textBuilder.Clear();
            textBuilder.Append("【レギュレーション違反】");

            foreach (var (type, label) in warningLabels)
            {
                if (!summaries.TryGetValue(type, out var summary) || summary.NoteCount == 0) { continue; }

                textBuilder.Append('\n').Append(label).Append(' ').Append(summary.NoteCount).Append("個");
                summary.AppendLocations(textBuilder, MAX_LOCATION_COUNT);
            }

            return textBuilder.ToString();
        }

        /// <summary>
        /// ログ欄の表示を更新する
        /// </summary>
        private void Display(string text)
        {
            displayedText = text;

            // 違反が無くなったら消す
            if (string.IsNullOrEmpty(text))
            {
                notice.Hide();
                return;
            }

            notice.Show(text);
            notice.KeepOnTop();
        }

        /// <summary>
        /// 違反の種類ごとの集計 (ノーツの個数と、発生している場所)
        /// </summary>
        private class WarningSummary
        {
            readonly List<(int BarIndex, int SubDivisionIndex)> locations = new List<(int, int)>();

            public int NoteCount { get; private set; }

            public void Clear()
            {
                NoteCount = 0;
                locations.Clear();
            }

            public void Add(int barIndex, int subDivisionIndex)
            {
                NoteCount++;

                // 同じ分線上の違反は、場所としては1つにまとめる
                var location = (barIndex, subDivisionIndex);
                if (!locations.Contains(location)) { locations.Add(location); }
            }

            /// <summary>
            /// 場所を譜面の先頭から順に書き出す。多いときは残りの数だけ書く
            /// </summary>
            public void AppendLocations(StringBuilder builder, int maxCount)
            {
                locations.Sort();

                for (int i = 0; i < locations.Count && i < maxCount; i++)
                {
                    // 画面の小節番号に合わせて1始まり。分線は小節線を1本目として数える
                    builder.Append("\n ・").Append(locations[i].BarIndex + 1).Append("小節目の")
                        .Append(locations[i].SubDivisionIndex + 1).Append("本目");
                }

                if (locations.Count > maxCount)
                {
                    builder.Append("\n ・ほか").Append(locations.Count - maxCount).Append("か所");
                }
            }
        }
    }
}

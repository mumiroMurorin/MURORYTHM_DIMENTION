using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 完成した SpaceHold (始点と終点がある) に、Shift を押しながらノーツを接続したとき、
    /// 加わるノーツの形を、その時刻での SpaceHold の断面 (前後の節を補間した形) に合わせる
    ///
    /// 接続しても、SpaceHold の帯の形がほとんど変わらないので、そこから少しだけ形を直す、という使い方ができる。
    ///
    /// 決まりごと:
    /// ・形を合わせるのは、どこにもつながっていない、ばらばらのスペースホールドのノーツだけ
    ///   (2つ以上つながっているものは、それ自体が完成した SpaceHold なので、形を変えない)
    /// ・接続に関わる完成した SpaceHold が、ちょうど1本のときだけ (2本をつなぐときは、どちらに合わせるか決まらないので、何もしない)
    /// ・SpaceHold の始点より前、終点より後ろにあるノーツは、補間できないので、形を変えない
    /// ・補間した形は、ゲームと同じやり方で作る。重なった点と、まっすぐな辺の途中の点は取り除く
    ///   (外周の形は同じ。帯の面の張り方は、ごくわずかに変わることがある)
    /// </summary>
    public static class SpaceHoldConnectCorrection
    {
        /// <summary>
        /// 形を合わせるノーツ1つぶん
        /// </summary>
        public class Correction
        {
            public NoteData_SpaceHold Note;
            public Vector2[] Before;
            public Vector2[] After;
        }

        /// <summary>
        /// 形を合わせるキーが押されているか (Shift)
        /// </summary>
        public static bool IsRequested => ModifierKey.Shift;

        /// <summary>
        /// 形を合わせるノーツと、合わせた後の形を決める
        /// </summary>
        /// <param name="joiningNotes">接続するノーツ (今つながっているノーツは、つながりごと調べる)</param>
        /// <param name="result">形を合わせるノーツが入る</param>
        /// <returns>1つでも形を合わせるとき true</returns>
        public static bool TryPlan(INotesDataGetter notesGetter, ChartData chartData, IEnumerable<IChainNoteData> joiningNotes, List<Correction> result)
        {
            result.Clear();
            if (notesGetter == null || chartData == null || joiningNotes == null) { return false; }

            var timeline = SpaceHoldTimeline.Get(chartData);
            if (timeline == null) { return false; }

            // 関わる SpaceHold のつながりを、重ならないように集める
            var chains = new List<List<NoteData_SpaceHold>>();
            var seen = new HashSet<NoteData_SpaceHold>();
            foreach (var note in joiningNotes)
            {
                if (note is not NoteData_SpaceHold spaceHold || seen.Contains(spaceHold)) { continue; }

                var chain = new List<NoteData_SpaceHold>();
                SpaceHoldSections.GetChain(notesGetter, spaceHold, chain);
                foreach (var chained in chain) { seen.Add(chained); }
                chains.Add(chain);
            }

            // 完成した SpaceHold が、ちょうど1本のときだけ
            List<NoteData_SpaceHold> complete = null;
            int completeCount = 0;
            foreach (var chain in chains)
            {
                if (!SpaceHoldSections.IsComplete(chain)) { continue; }

                complete = chain;
                completeCount++;
            }

            if (completeCount != 1)
            {
                Debug.Log(completeCount == 0
                    ? "【接続】形を合わせる SpaceHold がありません。始点と終点のそろった SpaceHold に、Shift を押しながら接続してください"
                    : "【接続】完成した SpaceHold どうしをつなぐときは、形を合わせません");
                return false;
            }

            var sections = new List<SpaceHoldSection>();
            if (!SpaceHoldSections.TryBuildSections(timeline, complete, sections)) { return false; }

            int outOfRangeCount = 0;
            foreach (var chain in chains)
            {
                if (ReferenceEquals(chain, complete) || chain.Count != 1) { continue; }

                NoteData_SpaceHold note = chain[0];
                if (!timeline.TryGet(note.Address, out float time, out float depth)) { continue; }

                if (!SpaceHoldSections.TryInterpolate(sections, time, depth, out Vector2[] vertices))
                {
                    outOfRangeCount++;
                    continue;
                }

                result.Add(new Correction
                {
                    Note = note,
                    Before = SpaceHoldSections.GetVertices(note.SpaceVertices),
                    After = vertices,
                });
            }

            if (outOfRangeCount > 0)
            {
                Debug.Log($"【接続】SpaceHold の始点より前・終点より後ろのノーツ ({outOfRangeCount} 個) は、補間できないので、形を変えませんでした");
            }

            return result.Count > 0;
        }

        /// <summary>
        /// 合わせた後の形にする
        /// </summary>
        public static void ApplyAfter(List<Correction> corrections)
        {
            foreach (var correction in corrections) { correction.Note.SpaceVertices.SetVertices(correction.After); }
        }

        /// <summary>
        /// 合わせる前の形に戻す
        /// </summary>
        public static void ApplyBefore(List<Correction> corrections)
        {
            foreach (var correction in corrections) { correction.Note.SpaceVertices.SetVertices(correction.Before); }
        }
    }
}

using System;
using System.Collections.Generic;

namespace ChartEditor
{
    /// <summary>
    /// 複数選択したホールド(スペースホールド)を、1回の操作でまとめて接続するときの段取りを決める
    ///
    /// 「どのノーツを」「どの接続番号に入れるか」を決めるだけで、実際の接続は NoteConnecter が行う。
    /// </summary>
    public static class ChainConnectPlanner
    {
        // どこにも接続されていないノーツの接続番号
        public const int UNCHAINED_INDEX = -1;

        /// <summary>
        /// 複数選択しているノーツの1つを押したときに、まとめて接続するノーツと接続番号を決める
        /// </summary>
        /// <param name="clickedNote">押したノーツ</param>
        /// <param name="selectingNotes">選択中のノーツ</param>
        /// <param name="isConnectable">そのノーツを接続の対象にできるか (譜面上にオブジェクトがあるか)</param>
        /// <param name="getChainNotes">接続番号から、その接続に入っているノーツを返す処理 (その番号の接続が無ければ null)</param>
        /// <param name="getUsableChainIndex">まだ使われていない接続番号を返す処理</param>
        /// <param name="members">接続するノーツが入る (選択中のノーツと、それらにすでにつながっているノーツ)</param>
        /// <param name="originIndices">members のそれぞれの、今の接続番号が入る (元に戻すとき用)</param>
        /// <param name="chainIndex">members を入れる接続番号</param>
        /// <returns>
        /// まとめて接続するとき true。
        /// 押したノーツが選択中でない、選択中の接続できるノーツが1つだけ、すでに全部つながっている、のどれかのとき false
        /// </returns>
        public static bool TryPlan(
            IChainNoteData clickedNote,
            IEnumerable<IDeployableNoteData> selectingNotes,
            Func<IChainNoteData, bool> isConnectable,
            Func<int, IEnumerable<IChainNoteData>> getChainNotes,
            Func<int> getUsableChainIndex,
            List<IChainNoteData> members,
            List<int> originIndices,
            out int chainIndex)
        {
            members.Clear();
            originIndices.Clear();
            chainIndex = UNCHAINED_INDEX;

            if (clickedNote == null || selectingNotes == null) { return false; }

            // 選択中のノーツのうち、押したノーツと同じ種類のもの (ホールド同士、スペースホールド同士)
            var targets = new List<IChainNoteData>();
            bool isClickedSelecting = false;
            foreach (var selecting in selectingNotes)
            {
                if (selecting is not IChainNoteData chainNote) { continue; }
                if (chainNote.GetType() != clickedNote.GetType()) { continue; }
                if (isConnectable != null && !isConnectable(chainNote)) { continue; }
                if (targets.Contains(chainNote)) { continue; }

                if (ReferenceEquals(chainNote, clickedNote)) { isClickedSelecting = true; }
                targets.Add(chainNote);
            }

            // 押したノーツが選択中で、ほかにも接続できるノーツが選択されているときだけ
            if (!isClickedSelecting || targets.Count < 2) { return false; }

            // 接続番号: 押したノーツがすでに接続されていれば、その番号
            // 未接続なら、選択中のノーツのうち最初に見つかった接続済みのノーツの番号。どれも未接続なら新しい番号
            chainIndex = clickedNote.ChainIndex.Value;
            if (chainIndex == UNCHAINED_INDEX)
            {
                foreach (var target in targets)
                {
                    if (target.ChainIndex.Value == UNCHAINED_INDEX) { continue; }

                    chainIndex = target.ChainIndex.Value;
                    break;
                }
            }

            if (chainIndex == UNCHAINED_INDEX) { chainIndex = getUsableChainIndex(); }

            // 接続するノーツ: 選択中のノーツと、それらにすでにつながっているノーツ
            // (つながっているノーツは、まとめて同じ接続に入れる。1つずつ押して接続するときと同じ)
            foreach (var target in targets)
            {
                AddMember(target, members, originIndices);

                int targetChainIndex = target.ChainIndex.Value;
                if (targetChainIndex == UNCHAINED_INDEX) { continue; }

                var chainNotes = getChainNotes != null ? getChainNotes(targetChainIndex) : null;
                if (chainNotes == null) { continue; }

                foreach (var chained in chainNotes)
                {
                    // 同じ番号がほかの種類のノーツに使われている場合(古い譜面)は、巻き込まない
                    if (chained == null || chained.GetType() != clickedNote.GetType()) { continue; }

                    AddMember(chained, members, originIndices);
                }
            }

            // すでに全部が同じ接続に入っているときは、接続するものが無い
            foreach (int originIndex in originIndices)
            {
                if (originIndex != chainIndex) { return true; }
            }

            members.Clear();
            originIndices.Clear();
            return false;
        }

        /// <summary>
        /// 選択中のノーツを、種類ごと (ホールド同士、スペースホールド同士) にまとめて接続するときの段取りを決める
        /// (押したノーツが無い操作 = ショートカットキー用。種類ごとに、最初に選択されているノーツを「押したノーツ」として扱う)
        /// </summary>
        /// <param name="selectingNotes">選択中のノーツ</param>
        /// <param name="isConnectable">そのノーツを接続の対象にできるか (譜面上にオブジェクトがあるか)</param>
        /// <param name="getChainNotes">接続番号から、その接続に入っているノーツを返す処理 (その番号の接続が無ければ null)</param>
        /// <param name="isChainIndexUsed">その接続番号が、すでに使われているか</param>
        /// <param name="members">接続するノーツが入る</param>
        /// <param name="originIndices">members のそれぞれの、今の接続番号が入る (元に戻すとき用)</param>
        /// <param name="targetIndices">members のそれぞれを入れる接続番号が入る</param>
        /// <returns>接続するものが1つでもあるとき true</returns>
        public static bool TryPlanSelection(
            IEnumerable<IDeployableNoteData> selectingNotes,
            Func<IChainNoteData, bool> isConnectable,
            Func<int, IEnumerable<IChainNoteData>> getChainNotes,
            Func<int, bool> isChainIndexUsed,
            List<IChainNoteData> members,
            List<int> originIndices,
            List<int> targetIndices)
        {
            members.Clear();
            originIndices.Clear();
            targetIndices.Clear();

            if (selectingNotes == null) { return false; }

            // 種類ごとに、基準にするノーツ (その種類で最初に選択されているもの) を決める
            var anchors = new List<IChainNoteData>();
            foreach (var selecting in selectingNotes)
            {
                if (selecting is not IChainNoteData chainNote) { continue; }
                if (isConnectable != null && !isConnectable(chainNote)) { continue; }

                bool isKnownKind = false;
                foreach (var anchor in anchors)
                {
                    if (anchor.GetType() == chainNote.GetType()) { isKnownKind = true; break; }
                }
                if (!isKnownKind) { anchors.Add(chainNote); }
            }

            // 新しい接続番号は、種類ごとに別のものにする
            // (番号の空きは、実際に接続するまで埋まらない。同じ空き番号を2回使うと、ホールドとスペースホールドが同じ番号になってしまう)
            var reservedIndices = new HashSet<int>();
            Func<int> getUsableChainIndex = () =>
            {
                int index = 0;
                while ((isChainIndexUsed != null && isChainIndexUsed(index)) || reservedIndices.Contains(index)) { index++; }
                reservedIndices.Add(index);
                return index;
            };

            var kindMembers = new List<IChainNoteData>();
            var kindOriginIndices = new List<int>();
            foreach (var anchor in anchors)
            {
                if (!TryPlan(anchor, selectingNotes, isConnectable, getChainNotes, getUsableChainIndex, kindMembers, kindOriginIndices, out int chainIndex))
                {
                    continue;
                }

                for (int i = 0; i < kindMembers.Count; i++)
                {
                    members.Add(kindMembers[i]);
                    originIndices.Add(kindOriginIndices[i]);
                    targetIndices.Add(chainIndex);
                }
            }

            return members.Count > 0;
        }

        /// <summary>
        /// 接続するノーツの一覧に、ノーツと今の接続番号を加える (すでに入っているノーツは加えない)
        /// </summary>
        private static void AddMember(IChainNoteData note, List<IChainNoteData> members, List<int> originIndices)
        {
            if (members.Contains(note)) { return; }

            members.Add(note);
            originIndices.Add(note.ChainIndex.Value);
        }
    }
}

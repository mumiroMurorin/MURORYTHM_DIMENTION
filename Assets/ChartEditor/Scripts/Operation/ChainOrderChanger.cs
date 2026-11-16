using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;
using static UndoRedo.History;

namespace ChartEditor
{
    /// <summary>
    /// 同じ分線に並んでいる、同じホールド(スペースホールド)のノーツの順番を入れ替える
    ///
    /// ホールドを同じ時刻で直角に曲げるときは、同じ分線に同じホールドのノーツを2つ以上置く。
    /// どちらが先につながるかは、ノーツが持っている順番で決まる (後からその分線に来たノーツが後ろ)。
    /// 入れ替えたいときは、ノーツを選んで ↑ / ↓ を押す。
    /// ・同じ分線に2つ並んでいるとき: どちらか(または両方)を選んで ↑ か ↓ を押すと、2つが入れ替わる
    /// ・3つ以上並んでいるとき: 1つ選んで、↑ で1つ後ろへ (つながりの終点側へ)、↓ で1つ前へ (始点側へ)
    /// レーン画面では、順番が後ろのノーツほど少し上にずらして表示されるので、入れ替わったことが見て分かる。
    ///
    /// ↑ / ↓ は、頂点レイヤーでは「つながっている次・前のノーツの頂点へ移る」操作に使われている (InputHandler)。
    /// こちらはグラウンド・スペースのレイヤーと全画面プレビューでだけ働くので、ぶつからない。
    /// </summary>
    public class ChainOrderChanger : ITickable
    {
        // 操作の途中なので、順番の入れ替えを受け付けないモード
        static readonly EditMode[] ignoreEditModes =
        {
            EditMode.Connecting,
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Moving,
            EditMode.Scaling,
            EditMode.SpaceMoving,
        };

        readonly IChartEditorDataGetter dataGetter;
        readonly INotesDataGetter notesGetter;

        public ChainOrderChanger(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
        }

        void ITickable.Tick()
        {
            int direction = 0;
            if (Input.GetKeyDown(KeyCode.UpArrow)) { direction = 1; }
            else if (Input.GetKeyDown(KeyCode.DownArrow)) { direction = -1; }

            if (direction == 0) { return; }

            // 読み込み中・文字の入力中・ほかの操作の途中は受け付けない
            // (頂点レイヤーも対象外。そこでは ↑ / ↓ は別の操作に使われている)
            if (!SelectedNotesKeyOperation.IsAcceptable(dataGetter, ignoreEditModes)) { return; }

            if (!TryFindSwapPair(notesGetter.SelectingNotes, notesGetter.GetChainNoteList, direction, out var note, out var other)) { return; }
            // ロックしたノーツの順番は変えない
            if (NoteLock.IsLocked(note) || NoteLock.IsLocked(other))
            {
                NoteLock.LogRefused(1, "入れ替え");
                return;
            }

            // 入れ替える。元に戻すときも、同じ2つをもう一度入れ替えればよい
            Record(() => Swap(note, other), () => Swap(note, other));

            Debug.Log($"【接続】同じ分線のノーツの順番を入れ替えました: {note.Address}");
        }

        /// <summary>
        /// 選択中のノーツから、順番を入れ替える2つのノーツを決める
        /// </summary>
        /// <param name="selectingNotes">選択中のノーツ</param>
        /// <param name="getChainList">接続番号から、その接続のノーツの並びを返す処理 (無ければ null)</param>
        /// <param name="direction">1 = 後ろへ (↑)、-1 = 前へ (↓)</param>
        /// <returns>入れ替える2つが決まったとき true</returns>
        public static bool TryFindSwapPair(
            IEnumerable<IDeployableNoteData> selectingNotes,
            Func<int, SortedChainNoteDataList> getChainList,
            int direction,
            out IChainNoteData note,
            out IChainNoteData other)
        {
            note = null;
            other = null;
            if (selectingNotes == null || getChainList == null) { return false; }

            // 選択中のノーツ (3つ以上選んでいると、どれを入れ替えるのか決められない)
            IChainNoteData first = null;
            IChainNoteData second = null;
            int count = 0;
            foreach (var selecting in selectingNotes)
            {
                count++;
                if (count > 2) { return false; }
                if (selecting is not IChainNoteData chainNote) { return false; }

                if (count == 1) { first = chainNote; }
                else { second = chainNote; }
            }

            if (first == null) { return false; }

            var chainList = getChainList(first.ChainIndex.Value);
            if (chainList == null) { return false; }

            int index = chainList.IndexOf(first);
            if (index < 0) { return false; }

            // 2つ選んでいるとき: 同じ接続の、同じ分線で隣り合っている2つなら、その2つを入れ替える
            if (second != null)
            {
                int secondIndex = chainList.IndexOf(second);
                if (secondIndex < 0 || Mathf.Abs(secondIndex - index) != 1) { return false; }
                if (!SortedChainNoteDataList.IsSameSubdivision(first, second)) { return false; }

                note = first;
                other = second;
                return true;
            }

            // 1つ選んでいるとき: 同じ分線に並んでいる仲間の範囲 (つながりの並びの中で、何番目から何番目までか)
            var notes = chainList.ChainNoteList;
            int runStart = index;
            int runEnd = index;
            while (runStart > 0 && SortedChainNoteDataList.IsSameSubdivision(notes[runStart - 1], first)) { runStart--; }
            while (runEnd < notes.Count - 1 && SortedChainNoteDataList.IsSameSubdivision(notes[runEnd + 1], first)) { runEnd++; }

            // 同じ分線に仲間がいない
            if (runStart == runEnd) { return false; }

            int otherIndex;
            if (runEnd - runStart == 1)
            {
                // 2つだけ並んでいるとき: ↑ でも ↓ でも、その2つを入れ替える
                // (端にいる方を選んで「もう動けない向き」のキーを押したときに、何も起きないのを避ける)
                otherIndex = index == runStart ? runEnd : runStart;
            }
            else
            {
                // 3つ以上並んでいるとき: 押した向きの隣と入れ替える。端にいて、その向きに仲間がいなければ何もしない
                otherIndex = index + (direction > 0 ? 1 : -1);
                if (otherIndex < runStart || otherIndex > runEnd) { return false; }
            }

            note = first;
            other = notes[otherIndex];
            return true;
        }

        private void Swap(IChainNoteData note, IChainNoteData other)
        {
            // 実行する時点の接続から探し直す (元に戻す・やり直すの間に、接続が変わっていることがある)
            var chainList = notesGetter.GetChainNoteList(note.ChainIndex.Value);
            if (chainList == null) { return; }

            chainList.SwapOrder(note, other);
        }
    }
}

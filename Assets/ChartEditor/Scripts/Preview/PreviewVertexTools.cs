using System.Collections.Generic;
using UnityEngine;
using static UndoRedo.History;

namespace ChartEditor
{
    /// <summary>
    /// 全画面プレビューの、頂点の道具
    /// (番号は、シーンの道具のボタンに保存されているので、後から変えない)
    /// </summary>
    public enum PreviewVertexTool
    {
        /// <summary>道具を選んでいない。ふつうの操作 (ノーツを選ぶ・動かす、頂点のつまみを動かす)</summary>
        Move = 0,
        /// <summary>編集: 押した位置に頂点を置く。頂点を右クリックすると、その頂点を消す</summary>
        Edit = 1,
        // 2 は、使っていない
        /// <summary>回転: 頂点を掴んで、頂点すべてを中心のまわりに回す</summary>
        Rotate = 3,
        /// <summary>拡大縮小: 頂点を掴んで、頂点すべてを中心から拡大・縮小する</summary>
        Scale = 4,
    }

    /// <summary>
    /// 全画面プレビューで、プレビューを見ながら、スペースのノーツ (スペースブレイク、スペースホールドの節) の頂点を編集する道具
    ///
    /// 頂点を持つノーツを1つだけ選ぶと、画面の左上に、頂点レイヤーと同じ見た目の道具の列が出る
    /// (PreviewVertexToolsView。出し入れは EditorMenuPresenter が行う)。
    /// ・道具を選んでいない間 (最初の状態): 今までどおり。頂点のつまみを動かす、◆ (中心点) で全体を動かす、ほかのノーツを選ぶ
    /// ・編集: クリックした所に頂点を置く。頂点を右クリックすると、その頂点を消す
    /// ・回転 / 拡大縮小: 頂点をドラッグすると、頂点すべてが、中心のまわりに回る / 中心から拡大・縮小する
    /// ・頂点スライド (頂点の番号をずらす) と、反転は、押したその場で行う
    /// ・頂点は、クリック・Ctrl+クリック・範囲のドラッグで選べる (選んだ頂点は、まとめて動く。PreviewVertexHandles)
    ///   頂点を選んでいる間に Delete を押すと、ノーツではなく、選んだ頂点が消える (頂点レイヤーと同じ。NoteDestroyer)
    /// 今の状態でできる操作は、画面の右側の操作説明の欄に出る (OperationDescriptionView の「プレビューの状態」)。
    /// 選んでいる道具をもう一度押すと、選んでいない状態に戻る。選択を変えたり、編集画面へ戻ったりして、列が消えるときも戻る。
    /// どの操作も、元に戻せる (1回の操作が、1回ぶん)。
    ///
    /// 編集画面の頂点レイヤーの道具と同じことをするが、あちらはエディットモードで道具を切り替えている。
    /// 全画面プレビューでは、エディットモードを Preview のままにしておく必要があるので、道具をここで別に持つ。
    /// また、プレビューでのクリックは、ふだんは「ノーツを選ぶ・選択を外す」なので、頂点を置くのは「編集」を選んでいる間だけにしてある。
    /// </summary>
    public static class PreviewVertexTools
    {
        /// <summary>形を作るのに必要な、頂点の数 (これより少なくは、消せない)</summary>
        public const int MIN_VERTEX_COUNT = 3;

        /// <summary>道具の列の右端の位置 (画面の左端からの距離。譜面情報の欄が、この右へよける)</summary>
        public const float LIST_RIGHT_EDGE = 145f;

        /// <summary>今選んでいる道具 (選んでいないときは Move)</summary>
        public static PreviewVertexTool Current { get; private set; } = PreviewVertexTool.Move;

        /// <summary>道具の列が、今出ているか</summary>
        public static bool IsListShown { get; set; }

        public static void Select(PreviewVertexTool tool)
        {
            Current = tool;
        }

        #region 選択中の頂点

        // 選択中の頂点 (PreviewVertexHandles が持っている並びを、そのまま見る) と、その頂点を持つノーツの頂点の並び
        static SpaceVertices selectionOwner;
        static List<VertexData> selection;

        /// <summary>全画面プレビューで、頂点を選んでいるか</summary>
        public static bool HasSelectedVertices => selectionOwner != null && selection != null && selection.Count > 0;

        /// <summary>
        /// 選択中の頂点の並びを知らせる (PreviewVertexHandles から。頂点を選べるノーツが無いときは owner = null)
        /// </summary>
        public static void ShareSelection(SpaceVertices owner, List<VertexData> selectedVertices)
        {
            selectionOwner = owner;
            selection = selectedVertices;
        }

        /// <summary>
        /// 選択中の頂点を消す (頂点レイヤーで、頂点を選んで Delete を押したときと同じ。元に戻すのは1回ぶん)
        /// </summary>
        /// <returns>消したとき true (頂点を選んでいないとき、消すと形を作れなくなるときは、消さずに false)</returns>
        public static bool RemoveSelectedVertices()
        {
            if (!HasSelectedVertices) { return false; }

            SpaceVertices vertices = selectionOwner;

            // 今もその並びの中にある頂点だけ (番号の小さい順)
            var ascending = new List<VertexData>();
            foreach (var vertex in selection)
            {
                int index = vertex.VertexIndex;
                if (index >= 0 && index < vertices.Vertices.Count && ReferenceEquals(vertices.Vertices[index], vertex)) { ascending.Add(vertex); }
            }
            if (ascending.Count == 0) { return false; }
            if (vertices.Vertices.Count - ascending.Count < MIN_VERTEX_COUNT) { return false; }

            ascending.Sort((a, b) => a.VertexIndex.CompareTo(b.VertexIndex));
            var descending = new List<VertexData>(ascending);
            descending.Reverse();

            selection.Clear();

            // 番号の大きい順に消して、小さい順に戻す (戻すとき、それぞれが、消したときの番号の場所へ入るように)
            Record(() =>
            {
                foreach (var vertex in descending) { vertices.RemoveVertex(vertex); }
            }, () =>
            {
                foreach (var vertex in ascending) { vertices.AddVertex(vertex); }
            });

            return true;
        }

        #endregion

        /// <summary>
        /// 道具を使える状態なら、対象のノーツと、その頂点の並びを返す
        /// (全画面プレビューで、頂点を持つノーツを、1つだけ選んでいるとき)
        /// </summary>
        public static bool TryGetTarget(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter, out IDeployableNoteData note, out SpaceVertices vertices)
        {
            note = null;
            vertices = null;

            if (dataGetter == null || notesGetter == null) { return false; }
            if (dataGetter.EditNoteType.Value != EditNoteType.Preview) { return false; }
            if (dataGetter.IsLoadingChart.Value) { return false; }

            return TryGetSingleVerticesNote(notesGetter.SelectingNotes, out note, out vertices);
        }

        /// <summary>
        /// 選択中のノーツが1つだけで、それが頂点を持つノーツなら、そのノーツと頂点の並びを返す
        /// </summary>
        public static bool TryGetSingleVerticesNote(IEnumerable<IDeployableNoteData> selectingNotes, out IDeployableNoteData note, out SpaceVertices vertices)
        {
            note = null;
            vertices = null;
            if (selectingNotes == null) { return false; }

            IDeployableNoteData single = null;
            int count = 0;
            foreach (var selecting in selectingNotes)
            {
                single = selecting;
                count++;
                if (count > 1) { return false; }
            }

            if (count != 1) { return false; }
            if (single is not IVerticesControlableNoteData verticesNote) { return false; }
            if (verticesNote.SpaceVertices == null || verticesNote.SpaceVertices.Vertices.Count == 0) { return false; }

            note = single;
            vertices = verticesNote.SpaceVertices;
            return true;
        }

        /// <summary>
        /// 頂点を1つ足す (並びの中の入る場所は、近い頂点から決まる。頂点レイヤーで置いたときと同じ)
        /// </summary>
        /// <param name="position">置く位置 (単位円の中の座標。外なら、縁に寄せられる)</param>
        public static VertexData AddVertex(SpaceVertices vertices, Vector2 position)
        {
            var vertex = new VertexData(position);

            Record(() =>
            {
                vertices.AddVertex(vertex);
            }, () =>
            {
                vertices.RemoveVertex(vertex);
            });

            return vertex;
        }

        /// <summary>
        /// 頂点を1つ足す。元に戻す履歴には、まだ登録しない (置いてから位置を調整し終えたときに、RecordAddedVertex で1回ぶん登録する)
        /// </summary>
        /// <param name="position">置く位置 (単位円の中の座標。外なら、縁に寄せられる)</param>
        public static VertexData AddVertexWithoutRecord(SpaceVertices vertices, Vector2 position)
        {
            var vertex = new VertexData(position);
            vertices.AddVertex(vertex);
            return vertex;
        }

        /// <summary>
        /// AddVertexWithoutRecord で足した頂点を、元に戻す履歴に登録する (今の位置に置いた、1回の操作として)
        /// </summary>
        public static void RecordAddedVertex(SpaceVertices vertices, VertexData vertex)
        {
            if (vertices == null || vertex == null) { return; }

            // もう足してあるので、ここでは行わない (execute: false)
            Record(() =>
            {
                // やり直し: 消したときの番号の場所へ、置き直す (位置は、調整し終えたときのまま)
                vertices.AddVertex(vertex);
            }, () =>
            {
                vertices.RemoveVertex(vertex);
            }, false);
        }

        /// <summary>
        /// 頂点を1つ消す
        /// </summary>
        /// <returns>消したとき true (形を作れなくなるとき、その並びの頂点でないときは、消さずに false)</returns>
        public static bool RemoveVertex(SpaceVertices vertices, VertexData vertex)
        {
            if (vertices == null || vertex == null) { return false; }
            if (vertices.Vertices.Count <= MIN_VERTEX_COUNT) { return false; }
            if (vertex.VertexIndex < 0 || vertex.VertexIndex >= vertices.Vertices.Count || !ReferenceEquals(vertices.Vertices[vertex.VertexIndex], vertex)) { return false; }

            Record(() =>
            {
                vertices.RemoveVertex(vertex);
            }, () =>
            {
                // 消したときの番号の場所へ戻る
                vertices.AddVertex(vertex);
            });

            return true;
        }

        /// <summary>
        /// 頂点の番号をずらす (頂点レイヤーの「頂点スライド」と同じ)
        /// </summary>
        public static void Slide(SpaceVertices vertices, int delta)
        {
            if (vertices == null || delta == 0) { return; }

            Record(() =>
            {
                vertices.SlideVertexIndices(delta);
            }, () =>
            {
                vertices.SlideVertexIndices(-delta);
            });
        }

        /// <summary>
        /// 形を反転する (頂点レイヤーの「反転」と同じ)
        /// </summary>
        /// <param name="isLeftRight">true = 左右を入れ替える (縦の軸で折り返す)、false = 上下を入れ替える (横の軸で折り返す)</param>
        public static void Mirror(SpaceVertices vertices, bool isLeftRight)
        {
            if (vertices == null) { return; }

            Vector2 axisA = isLeftRight ? new Vector2(0f, 1f) : new Vector2(1f, 0f);
            Vector2 axisB = -axisA;

            // もう一度折り返すと、元に戻る
            Record(() =>
            {
                vertices.ReverseVertices(axisA, axisB);
            }, () =>
            {
                vertices.ReverseVertices(axisA, axisB);
            });
        }
    }
}

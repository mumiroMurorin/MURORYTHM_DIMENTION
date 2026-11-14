using System;
using System.Collections.Generic;
using ChartEditor;

namespace ChartConvert
{
    /// <summary>
    /// 同じ分線(同じ時刻)に、同じホールド(スペースホールド)のノーツが複数あるときの「順番」を扱う
    ///
    /// ホールドを同じ時刻で直角に曲げる(段を付ける)ときは、同じ分線に、同じホールドのノーツを2つ以上置く。
    /// そのとき、どちらが先かを決める規則をここ1か所にまとめている。
    ///
    /// 【保存】ノーツに「同じ分線の中で何番目か」(OrderInSubdivision) を書く。書くのは、同じ分線に仲間がいるノーツだけ。
    /// 【読み込み】エディタへの読み込みも、ゲーム用の変換(プレビューも同じ)も、Split を通して同じ順番で読む。
    ///   ・番号が書いてあれば、その順
    ///   ・番号が無ければ、種類の順 (始点 → 中継点 → メッシュ中継点 → 終点)。同じ種類の中では、データに並んでいる順
    ///     (番号を書くようになる前の譜面が、今までと同じ形に読まれるようにするための規則)
    /// </summary>
    public static class ChainOrderResolver
    {
        // つながりをたどる回数の上限 (つながりが壊れていても、止まらなくならないように)
        const int MAX_WALK_COUNT = 4096;

        // グラウンドのホールドと、スペースホールド (接続番号は、それぞれの中で別々に数える)
        const int FAMILY_GROUND = 0;
        const int FAMILY_SPACE = 1;

        #region 保存

        /// <summary>
        /// エディタ上のノーツが、同じ分線にある同じ接続のノーツの中で、先頭から何番目か (0 が先頭)
        /// つながっている順をさかのぼって数える
        /// </summary>
        public static int GetIndexInSubdivision(IChainNoteData note)
        {
            if (note == null || note.NoteObject == null || note.Address == null) { return 0; }

            int index = 0;
            IConnectableObject back = note.NoteObject.BackNote.Value;
            while (back != null && index < MAX_WALK_COUNT && IsSameSubdivision(back, note))
            {
                index++;
                back = back.BackNote.Value;
            }

            return index;
        }

        /// <summary>
        /// 保存するときに書く「同じ分線の中で何番目か」を返す
        /// 同じ分線に、同じ接続のノーツがほかに無いときは null (その場合は何も書かない)
        /// </summary>
        public static int? GetExportOrder(IChainNoteData note)
        {
            if (note == null || note.NoteObject == null || note.Address == null) { return null; }

            int index = GetIndexInSubdivision(note);
            if (index > 0) { return index; }

            // 先頭のノーツ: 次のノーツが同じ分線にあるときだけ、番号を書く
            IConnectableObject next = note.NoteObject.NextNote.Value;
            if (next != null && IsSameSubdivision(next, note)) { return 0; }

            return null;
        }

        private static bool IsSameSubdivision(IConnectableObject other, IChainNoteData note)
        {
            IChainNoteData otherNote = other.ChainNoteData;
            if (otherNote == null) { return false; }

            var address = otherNote.Address;
            return address != null &&
                   address.BarIndex == note.Address.BarIndex &&
                   address.SubDivisionIndex == note.Address.SubDivisionIndex;
        }

        #endregion

        #region 読み込み

        // 分線の中の、つながるノーツ1つ
        private struct Node
        {
            public IChainNoteDataOrigin Entry;
            public int Family;
            // 種類の順 (番号が無いときの順番。始点 → 中継点 → メッシュ中継点 → 終点)
            public int TypeRank;
            // その種類の一覧の中で何番目か
            public int ListIndex;
            // 同じ接続のノーツの中で何番目か (求めた結果)
            public int Position;
        }

        // 作業用 (分線ごとに確保し直さないよう、使い回す。読み込みはメインスレッドでしか行わない)
        static readonly List<Node> nodes = new List<Node>();
        static readonly List<int> nodeOrder = new List<int>();

        /// <summary>
        /// 分線のデータを、順番どおりに読める形に分ける
        ///
        /// 同じ分線に、同じ接続のノーツが2つ以上あるときは、分線のデータを「1つ目だけ」「2つ目だけ」…に分けて返す。
        /// 返されたものを先頭から順に、今までと同じやり方で読めば、ノーツは決めた順番で読まれる。
        /// (つながらないノーツと、1つしか無い接続のノーツは、すべて先頭のデータに入る)
        /// ほとんどの分線では分ける必要が無く、その場合は、渡されたデータをそのまま1つだけ返す。
        /// </summary>
        /// <param name="origin">分線のデータ</param>
        /// <param name="parts">結果が入る (呼び出すたびに中身を入れ替える)</param>
        public static void Split(SubDivisionDataOrigin origin, List<SubDivisionDataOrigin> parts)
        {
            parts.Clear();
            parts.Add(origin);
            if (origin == null) { return; }

            // つながるノーツが、グラウンド・スペースのそれぞれで1つ以下なら、順番を決める必要は無い
            int groundCount = Count(origin.HoldStartData) + Count(origin.DivineHoldStartData) + Count(origin.HoldRelayData) +
                              Count(origin.HoldMeshRelayData) + Count(origin.HoldEndData) + Count(origin.HoldEndUnjudgeData);
            int spaceCount = Count(origin.SpaceHoldStartData) + Count(origin.SpaceHoldRelayData) +
                             Count(origin.SpaceHoldMeshRelayData) + Count(origin.SpaceHoldEndData);
            if (groundCount < 2 && spaceCount < 2) { return; }

            // つながるノーツを、種類の順・一覧の順に集める
            nodes.Clear();
            AddNodes(origin.HoldStartData, FAMILY_GROUND, 0);
            AddNodes(origin.DivineHoldStartData, FAMILY_GROUND, 1);
            AddNodes(origin.HoldRelayData, FAMILY_GROUND, 2);
            AddNodes(origin.HoldMeshRelayData, FAMILY_GROUND, 3);
            AddNodes(origin.HoldEndData, FAMILY_GROUND, 4);
            AddNodes(origin.HoldEndUnjudgeData, FAMILY_GROUND, 5);
            AddNodes(origin.SpaceHoldStartData, FAMILY_SPACE, 0);
            AddNodes(origin.SpaceHoldRelayData, FAMILY_SPACE, 1);
            AddNodes(origin.SpaceHoldMeshRelayData, FAMILY_SPACE, 2);
            AddNodes(origin.SpaceHoldEndData, FAMILY_SPACE, 3);

            // 同じ接続のノーツごとに、何番目かを決める
            int partCount = AssignPositions();
            if (partCount <= 1)
            {
                nodes.Clear();
                return;
            }

            // 分ける: 先頭のデータには、つながらないノーツもすべて入れる
            parts.Clear();
            for (int i = 0; i < partCount; i++)
            {
                var part = new SubDivisionDataOrigin
                {
                    Bpm = origin.Bpm,
                    SpeedRatio = origin.SpeedRatio,
                };

                if (i == 0)
                {
                    part.TouchNoteData = origin.TouchNoteData;
                    part.DivineTouchData = origin.DivineTouchData;
                    part.DynamicUpwardData = origin.DynamicUpwardData;
                    part.DynamicDownwardData = origin.DynamicDownwardData;
                    part.DynamicRightwardData = origin.DynamicRightwardData;
                    part.DynamicLeftwardData = origin.DynamicLeftwardData;
                    part.SpaceBreakData = origin.SpaceBreakData;
                }

                parts.Add(part);
            }

            // 集めた順(種類の順・一覧の順)のまま振り分けるので、それぞれのデータの中の並びは、元の並びと変わらない
            foreach (var node in nodes)
            {
                AddToPart(parts[node.Position], node.Entry);
            }

            nodes.Clear();
        }

        private static int Count<T>(List<T> list)
        {
            return list != null ? list.Count : 0;
        }

        private static void AddNodes<T>(List<T> list, int family, int typeRank) where T : IChainNoteDataOrigin
        {
            if (list == null) { return; }

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null) { continue; }

                nodes.Add(new Node { Entry = list[i], Family = family, TypeRank = typeRank, ListIndex = i });
            }
        }

        // 何番目かを付け終えたノーツ (nodes の中の位置)
        static readonly HashSet<int> assigned = new HashSet<int>();

        /// <summary>
        /// 集めたノーツのそれぞれに、同じ接続のノーツの中で何番目かを付ける
        /// </summary>
        /// <returns>いちばん多い接続のノーツの数 (= 分ける数)。1 以下なら、分ける必要は無い</returns>
        private static int AssignPositions()
        {
            int partCount = 1;
            assigned.Clear();

            for (int i = 0; i < nodes.Count; i++)
            {
                // 同じ接続の、先に出てきたノーツと一緒に付け終えている
                if (assigned.Contains(i)) { continue; }

                // 同じ接続のノーツを集める (集めた順 = 種類の順・一覧の順)
                nodeOrder.Clear();
                for (int j = i; j < nodes.Count; j++)
                {
                    if (nodes[j].Family != nodes[i].Family || nodes[j].Entry.HoldNumber != nodes[i].Entry.HoldNumber) { continue; }

                    nodeOrder.Add(j);
                    assigned.Add(j);
                }

                // この分線に1つしか無い接続は、0番目のまま
                if (nodeOrder.Count < 2) { continue; }

                // 順番: 番号が書いてあるノーツを番号順に先に、番号が無いノーツをその後に (どちらも、同じなら集めた順のまま)
                SortByExplicitOrder(nodeOrder);

                for (int position = 0; position < nodeOrder.Count; position++)
                {
                    Node node = nodes[nodeOrder[position]];
                    node.Position = position;
                    nodes[nodeOrder[position]] = node;
                }

                partCount = Math.Max(partCount, nodeOrder.Count);
            }

            assigned.Clear();
            nodeOrder.Clear();
            return partCount;
        }

        /// <summary>
        /// 同じ接続のノーツを、順番どおりに並べ替える (並びが同じ扱いのものは、元の順のまま = 安定な並べ替え)
        /// </summary>
        private static void SortByExplicitOrder(List<int> nodeIndices)
        {
            // 数が少ないので、単純な挿入ソートで足りる
            for (int i = 1; i < nodeIndices.Count; i++)
            {
                int current = nodeIndices[i];
                int j = i - 1;
                while (j >= 0 && IsExplicitlyBefore(current, nodeIndices[j]))
                {
                    nodeIndices[j + 1] = nodeIndices[j];
                    j--;
                }

                nodeIndices[j + 1] = current;
            }
        }

        /// <summary>
        /// 書いてある番号だけで見て、a が b より先か (番号が無いノーツ同士、番号が同じノーツ同士は「先ではない」)
        /// </summary>
        private static bool IsExplicitlyBefore(int nodeIndexA, int nodeIndexB)
        {
            int? orderA = nodes[nodeIndexA].Entry.OrderInSubdivision;
            int? orderB = nodes[nodeIndexB].Entry.OrderInSubdivision;

            // 番号があるノーツは、番号が無いノーツより先
            if (orderA.HasValue && !orderB.HasValue) { return true; }
            if (!orderA.HasValue) { return false; }

            return orderA.Value < orderB.Value;
        }

        private static void AddToPart(SubDivisionDataOrigin part, IChainNoteDataOrigin entry)
        {
            switch (entry)
            {
                case NoteDataOrigin_HoldStart e:
                    (part.HoldStartData ??= new List<NoteDataOrigin_HoldStart>()).Add(e);
                    break;
                case NoteDataOrigin_DivineHoldStart e:
                    (part.DivineHoldStartData ??= new List<NoteDataOrigin_DivineHoldStart>()).Add(e);
                    break;
                case NoteDataOrigin_HoldRelay e:
                    (part.HoldRelayData ??= new List<NoteDataOrigin_HoldRelay>()).Add(e);
                    break;
                case NoteDataOrigin_HoldMeshRelay e:
                    (part.HoldMeshRelayData ??= new List<NoteDataOrigin_HoldMeshRelay>()).Add(e);
                    break;
                case NoteDataOrigin_HoldEnd e:
                    (part.HoldEndData ??= new List<NoteDataOrigin_HoldEnd>()).Add(e);
                    break;
                case NoteDataOrigin_HoldEndUnjudge e:
                    (part.HoldEndUnjudgeData ??= new List<NoteDataOrigin_HoldEndUnjudge>()).Add(e);
                    break;
                case NoteDataOrigin_SpaceHoldStart e:
                    (part.SpaceHoldStartData ??= new List<NoteDataOrigin_SpaceHoldStart>()).Add(e);
                    break;
                case NoteDataOrigin_SpaceHoldRelay e:
                    (part.SpaceHoldRelayData ??= new List<NoteDataOrigin_SpaceHoldRelay>()).Add(e);
                    break;
                case NoteDataOrigin_SpaceHoldMeshRelay e:
                    (part.SpaceHoldMeshRelayData ??= new List<NoteDataOrigin_SpaceHoldMeshRelay>()).Add(e);
                    break;
                case NoteDataOrigin_SpaceHoldEnd e:
                    (part.SpaceHoldEndData ??= new List<NoteDataOrigin_SpaceHoldEnd>()).Add(e);
                    break;
            }
        }

        #endregion
    }
}

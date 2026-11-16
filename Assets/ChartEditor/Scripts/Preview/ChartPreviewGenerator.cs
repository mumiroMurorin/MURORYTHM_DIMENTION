using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using VContainer;
using Debug = UnityEngine.Debug;

namespace ChartEditor
{
    /// <summary>
    /// プレビュー用のノーツを生成する
    ///
    /// ・今あるノーツと新しい譜面データを「中身」で比べ、変わったノーツだけ作り直す
    /// ・生成と破棄は1フレームあたりの時間を区切って少しずつ行う (画面を止めない)
    /// ・再生位置に近いノーツから先に作る
    ///
    /// 生成そのものは本編と同じファクトリを使うので、見た目は本編と同じになる。
    /// </summary>
    public class ChartPreviewGenerator : MonoBehaviour, IChartGenerator, IChartDestroyer
    {
        [Serializable]
        private class NoteFactoryBinding
        {
            [SerializeField] private global::NoteType noteType;
            [SerializeField] private MonoBehaviour factory;

            public global::NoteType NoteType => noteType;
            public MonoBehaviour Factory => factory;
        }

        [Header("Note Factories")]
        [SerializeField] private NoteFactoryBinding[] noteFactories;

        [Header("Factory Initialization")]
        [SerializeField] private NoteJudgementSettingsCatalog judgementSettingsCatalog;
        [SerializeField] private Difficulty previewDifficulty = Difficulty.Normal;
        [SerializeField] private Transform noteParent;
        [SerializeField] private SerializeInterface<ITimeGetter> timer;
        [SerializeField] private PreviewNoteVisibilityController noteVisibilityController;

        [Header("生成の小分け")]
        [Tooltip("1フレームのうち、ノーツの生成に使ってよい時間(ミリ秒)。大きいほど早く終わるが、画面がカクつく")]
        [SerializeField] private float frameBudgetMilliseconds = 6f;
        [Tooltip("1フレームに破棄するノーツの最大数")]
        [SerializeField] private int maxDestroyCountPerFrame = 60;
        [Tooltip("再生位置がこの距離以上動いたら、生成する順番を並べ直す")]
        [SerializeField] private float resortDistance = 30f;
        [Tooltip("再生位置より手前(通過済み)でも、この距離までは先に生成する")]
        [SerializeField] private float priorityBehindDistance = 10f;

        /// <summary>
        /// 生成済みのノーツ
        /// </summary>
        private class SpawnedNote
        {
            public Component Component;
            public INoteVisibilityTarget Target;
            public INoteData Data;
            public PreviewNoteDataKey Key;

            // ファクトリが専用のメッシュを作る種類か (破棄するときにメッシュも破棄する)
            public bool HasGeneratedMeshes;

            // 同じ中身のノーツをまとめるための一時的な連結 (差分を取る間だけ使う)
            public SpawnedNote NextSameKey;
            // 差分を取る間、新しいデータと対応が付いたか
            public bool IsMatched;
        }

        /// <summary>
        /// ノーツの種類ごとの状態
        /// </summary>
        private class NoteTypeState
        {
            public global::NoteType NoteType;
            public MonoBehaviour Factory;
            public MethodInfo InitializeMethod;
            public MethodInfo SpawnMethod;

            // 生成結果がそれまでの生成に左右される種類か (IOrderDependentNoteFactory)
            public bool IsOrderDependent;

            // 今あるノーツ
            public List<SpawnedNote> LiveNotes = new List<SpawnedNote>();

            // --- IsOrderDependent のときだけ使う ---
            // 最後まで作り終えた「ノーツの並び」。これと同じなら作り直さない
            public bool HasCompletedSequence;
            public PreviewNoteDataKey CompletedSequenceKey;
            // 作り直している途中の並びと、残りの数
            public PreviewNoteDataKey PendingSequenceKey;
            public int PendingCount;
        }

        /// <summary>
        /// これから生成するノーツ
        /// </summary>
        private struct SpawnTask
        {
            public NoteTypeState TypeState;
            public INoteData Data;
            public PreviewNoteDataKey Key;
            // 譜面上の累積距離 (生成する順番を決めるのに使う)
            public float Distance;
            // 生成と同時に破棄する、置き換え前のノーツ (無ければ null)
            public SpawnedNote Replaces;
        }

        /// <summary>
        /// これから破棄するノーツ
        /// </summary>
        private struct DestroyTask
        {
            public GameObject NoteObject;
            public bool HasGeneratedMeshes;
        }

        /// <summary>
        /// Apply の結果
        /// </summary>
        public struct ApplyResult
        {
            // そのまま残したノーツの数
            public int KeptCount;
            // これから生成するノーツの数
            public int SpawnCount;
            // 破棄するノーツの数
            public int DiscardCount;
            // すべて作り直しになったか
            public bool IsFullRebuild;
        }

        private global::ChartData chartData;
        INoteSpawnDataOptionGetter optionHolder;

        readonly List<NoteTypeState> typeStates = new List<NoteTypeState>();
        NoteFactoryInitializingData factoryInitializingData;
        bool isFactoriesInitialized;

        // 位置の決まり方(速度倍率・ノーツ速度・軌道半径)を要約したキー。変わったら全部作り直す
        bool hasLayoutKey;
        PreviewNoteDataKey layoutKey;

        // 再生位置に近い順に生成するノーツ
        readonly List<SpawnTask> nearestQueue = new List<SpawnTask>();
        int nearestIndex;
        float sortedAtDistance;
        // 譜面順に生成するノーツ (IsOrderDependent の種類)
        readonly List<SpawnTask> orderedQueue = new List<SpawnTask>();
        int orderedIndex;
        bool isOrderedTurn;
        // 破棄待ちのノーツ (すでに非表示)
        readonly Queue<DestroyTask> destroyQueue = new Queue<DestroyTask>();

        // 作り直しの途中で次の更新が来たときに、続きから再開できるよう取っておく生成待ち
        readonly List<SpawnTask> interruptedOrderedTasks = new List<SpawnTask>();

        // 差分を取るときの作業用 (毎回確保しないよう使い回す)
        readonly Dictionary<PreviewNoteDataKey, SpawnedNote> keyToLiveNote = new Dictionary<PreviewNoteDataKey, SpawnedNote>();
        readonly HashSet<INoteVisibilityTarget> discardedTargets = new HashSet<INoteVisibilityTarget>();
        readonly List<SpawnTask> plannedTasks = new List<SpawnTask>();
        readonly List<SpawnedNote> previousLiveNotes = new List<SpawnedNote>();
        readonly HashSet<GameObject> trackedNoteObjects = new HashSet<GameObject>();
        readonly List<MeshFilter> meshFilterBuffer = new List<MeshFilter>();
        // クリック判定で当たったノーツと、並べる順を決める値 (小さいほど先)
        readonly List<(float Order, INoteData Data)> pickCandidates = new List<(float, INoteData)>();
        readonly object[] spawnArguments = new object[2];
        readonly List<Renderer> pickRendererBuffer = new List<Renderer>();
        readonly List<MeshFilter> pickMeshFilterBuffer = new List<MeshFilter>();
        // 当たり判定で調べるメッシュの頂点と三角形 (調べるたびに配列を作らないよう、使い回す)
        readonly List<Vector3> pickVertexBuffer = new List<Vector3>();
        readonly List<int> pickTriangleBuffer = new List<int>();
        // 当たり判定に使っている光線と、「当たった場所が今プレビューに映っているか」を調べる処理 (毎回作り直さないよう使い回す)
        Ray pickRay;
        System.Predicate<float> isVisibleHit;
        readonly Stopwatch stopwatch = new Stopwatch();

        /// <summary>
        /// 今回の更新で生成するノーツの総数
        /// </summary>
        public int TotalSpawnCount { get; private set; }

        /// <summary>
        /// まだ生成していないノーツの数
        /// </summary>
        public int RemainingSpawnCount => (nearestQueue.Count - nearestIndex) + (orderedQueue.Count - orderedIndex);

        /// <summary>
        /// ノーツを生成している途中か
        /// </summary>
        public bool IsGenerating => RemainingSpawnCount > 0;

        /// <summary>
        /// ノーツの親 (ノーツと一緒に動く地面)
        /// </summary>
        public Transform NoteParent => noteParent;

        /// <summary>
        /// 今のプレビューで使っている「時刻 → 譜面上の位置」の計算。まだ譜面を反映していないときは null
        /// </summary>
        public INotePositionCalculator PositionCalculator => chartData?.PositionGraph;

        [Inject]
        public void Constructor(INoteSpawnDataOptionGetter optionHolder)
        {
            this.optionHolder = optionHolder;
        }

        private void Awake()
        {
            InitializeFactories();
        }

        #region 外部から呼ぶ処理

        public void SetChartData(global::ChartData chartData)
        {
            this.chartData = chartData;
        }

        /// <summary>
        /// セット済みの譜面データで、すべてのノーツを作り直す
        /// </summary>
        public void Generate(Action callback = null)
        {
            if (chartData == null)
            {
                Debug.LogWarning("[ChartPreviewGenerator] Chart data is null.");
                return;
            }

            Apply(chartData, true);
            callback?.Invoke();
        }

        /// <summary>
        /// すべてのノーツを破棄する
        /// </summary>
        public void DestroyChart()
        {
            ClearSpawnQueues();
            DiscardAllLiveNotes();
            hasLayoutKey = false;
        }

        /// <summary>
        /// 新しい譜面データをプレビューに反映する
        /// 今あるノーツと比べて、変わった分だけを生成・破棄の予定に入れる (実際の生成は Update で少しずつ行う)
        /// </summary>
        /// <param name="forceFullRebuild">true のとき、中身が同じノーツも含めてすべて作り直す</param>
        public ApplyResult Apply(global::ChartData newChartData, bool forceFullRebuild)
        {
            var result = new ApplyResult();
            if (newChartData == null) { return result; }

            InitializeFactories();
            chartData = newChartData;

            float noteSpeed = optionHolder?.NoteSpeed.Value ?? 1f;
            float curveRadius = optionHolder?.NoteCurveRadius.Value ?? 2000f;
            var positionGraph = newChartData.PositionGraph;

            // 生成途中だった分は捨てて、新しいデータで予定を立て直す
            // ただし譜面順に作り直している途中の分は、並びが変わっていなければ続きから再開できるよう取っておく
            interruptedOrderedTasks.Clear();
            for (int i = orderedIndex; i < orderedQueue.Count; i++)
            {
                interruptedOrderedTasks.Add(orderedQueue[i]);
            }
            ClearSpawnQueues();

            // 位置の決まり方が変わったら、すべてのノーツの位置が変わるので全部作り直す
            var newLayoutKey = BuildLayoutKey(positionGraph, noteSpeed, curveRadius);
            bool isLayoutChanged = !hasLayoutKey || !newLayoutKey.Equals(layoutKey);
            layoutKey = newLayoutKey;
            hasLayoutKey = true;

            if (forceFullRebuild || isLayoutChanged)
            {
                result.IsFullRebuild = true;
                result.DiscardCount += DiscardAllLiveNotes();
            }

            noteVisibilityController?.Initialize(positionGraph, noteSpeed, curveRadius);

            // 種類ごとに、今あるノーツと新しいデータを比べる
            foreach (var typeState in typeStates)
            {
                var noteDataList = newChartData.GetNoteDataList(typeState.NoteType);

                if (typeState.IsOrderDependent)
                {
                    PlanOrderDependentType(typeState, noteDataList, positionGraph, noteSpeed, ref result);
                }
                else
                {
                    PlanIndependentType(typeState, noteDataList, positionGraph, noteSpeed, ref result);
                }
            }

            // 不要になったノーツをまとめて非表示にする (破棄そのものは少しずつ行う)
            if (discardedTargets.Count > 0)
            {
                noteVisibilityController?.RemoveAll(discardedTargets);
                discardedTargets.Clear();
            }

            interruptedOrderedTasks.Clear();
            SortNearestQueue();

            TotalSpawnCount = RemainingSpawnCount;
            result.SpawnCount = TotalSpawnCount;
            return result;
        }

        /// <summary>
        /// 光線が当たるノーツのデータを、選ばれやすい順に並べて返す (プレビュー上のクリック判定用)
        ///
        /// 先に単発のノーツを「小さく見えるものから順」に、その後にホールドの帯を手前から順に並べる。
        /// ・小さく見えるものを先にするのは、大きなノーツの中や手前に重なって見える小さなノーツを選べるようにするため
        ///   (スペースホールドの節は、手前の大きな節が、奥の節をすっぽり覆って見えることがある。
        ///    手前から順にすると、奥の節はどこをクリックしても選べない)
        /// ・帯を後にするのは、帯が画面の広い範囲を覆っていて、単発のノーツより先に当たってしまうため
        /// </summary>
        public void PickNotes(Ray ray, List<INoteData> results)
        {
            results.Clear();
            CollectPickedNotes(ray, false, results);
            CollectPickedNotes(ray, true, results);
        }

        private void CollectPickedNotes(Ray ray, bool isOrderDependentType, List<INoteData> results)
        {
            pickCandidates.Clear();

            foreach (var typeState in typeStates)
            {
                if (typeState.IsOrderDependent != isOrderDependentType) { continue; }

                foreach (var note in typeState.LiveNotes)
                {
                    if (note.Component == null || note.Data == null) { continue; }

                    // 表示中のノーツだけを対象にする (非表示のノーツは子オブジェクトが無効になっている)
                    Transform noteTransform = note.Component.transform;
                    if (noteTransform.childCount == 0 || !noteTransform.GetChild(0).gameObject.activeInHierarchy) { continue; }

                    // 地面に落ちる影は、当たり判定に含めない
                    Transform shadowRoot = GetShadowRoot(note.Data);

                    // 帯(ホールド)は、実際に見えている形に当たっているかを調べる
                    // (描画範囲の箱は、曲がりながら奥へ伸びる帯では見た目よりずっと大きい。
                    //  箱で判定すると、帯から離れた場所をクリックしても帯に当たってしまう)
                    if (isOrderDependentType)
                    {
                        if (TryGetVisibleMeshHitLength(ray, noteTransform, shadowRoot, out float meshHitLength, out bool hasReadableMesh))
                        {
                            pickCandidates.Add((meshHitLength, note.Data));
                            continue;
                        }

                        // 形は分かっているが、見えている部分には当たっていない
                        if (hasReadableMesh) { continue; }
                    }

                    // そのノーツの描画範囲のうち、光線が当たった部品について
                    // ・いちばん手前で当たる距離
                    // ・いちばん小さく見える部品の、見かけの大きさ (部品の大きさ ÷ 視点からの距離)
                    float nearestDistance = float.MaxValue;
                    float smallestApparentSize = float.MaxValue;
                    noteTransform.GetComponentsInChildren(false, pickRendererBuffer);
                    foreach (Renderer noteRenderer in pickRendererBuffer)
                    {
                        if (!noteRenderer.enabled) { continue; }
                        if (shadowRoot != null && noteRenderer.transform.IsChildOf(shadowRoot)) { continue; }

                        Bounds bounds = noteRenderer.bounds;
                        if (!bounds.IntersectRay(ray, out float distance)) { continue; }

                        nearestDistance = Mathf.Min(nearestDistance, distance);

                        float apparentSize = bounds.extents.magnitude / Mathf.Max(Vector3.Distance(ray.origin, bounds.center), 0.01f);
                        smallestApparentSize = Mathf.Min(smallestApparentSize, apparentSize);
                    }

                    if (nearestDistance == float.MaxValue) { continue; }

                    // 帯は手前から順、単発のノーツは小さく見えるものから順
                    pickCandidates.Add((isOrderDependentType ? nearestDistance : smallestApparentSize, note.Data));
                }
            }

            pickRendererBuffer.Clear();

            pickCandidates.Sort((a, b) => a.Order.CompareTo(b.Order));
            foreach (var candidate in pickCandidates)
            {
                results.Add(candidate.Data);
            }
            pickCandidates.Clear();
        }

        /// <summary>
        /// 今の再生位置 (譜面上の距離)
        /// </summary>
        public float CurrentDistance => noteVisibilityController != null ? noteVisibilityController.CurrentDistance : 0f;

        /// <summary>
        /// 譜面上の距離が、今プレビューにノーツを表示している範囲に入っているか
        /// </summary>
        public bool IsInVisibleRange(float distance)
        {
            return noteVisibilityController == null || noteVisibilityController.IsInVisibleRange(distance);
        }

        /// <summary>
        /// 光線が、指定したノーツの見た目のどこに当たっているかを求める (プレビュー上でノーツを掴む位置を決めるのに使う)
        /// 形(メッシュ)を読み取れるノーツは形との交点を、読み取れないノーツは描画範囲の箱との交点を返す
        /// </summary>
        /// <param name="data">PickNotes が返したノーツのデータ</param>
        public bool TryGetHitPoint(Ray ray, INoteData data, out Vector3 worldPoint)
        {
            worldPoint = default;
            if (data == null) { return false; }

            foreach (var typeState in typeStates)
            {
                foreach (var note in typeState.LiveNotes)
                {
                    if (!ReferenceEquals(note.Data, data) || note.Component == null) { continue; }

                    return TryGetHitPoint(ray, note.Component.transform, GetShadowRoot(data), out worldPoint);
                }
            }

            return false;
        }

        private bool TryGetHitPoint(Ray ray, Transform noteTransform, Transform shadowRoot, out Vector3 worldPoint)
        {
            worldPoint = default;
            float nearestLength = float.MaxValue;

            // 形との交点 (帯のように大きなノーツでも、実際に見えている面までの長さが分かる)
            if (TryGetVisibleMeshHitLength(ray, noteTransform, shadowRoot, out float meshHitLength, out _))
            {
                nearestLength = meshHitLength;
            }

            // 形を読み取れなかったときは、描画範囲の箱で代用する
            if (nearestLength == float.MaxValue)
            {
                noteTransform.GetComponentsInChildren(false, pickRendererBuffer);
                foreach (Renderer noteRenderer in pickRendererBuffer)
                {
                    if (!noteRenderer.enabled) { continue; }
                    if (shadowRoot != null && noteRenderer.transform.IsChildOf(shadowRoot)) { continue; }
                    if (!noteRenderer.bounds.IntersectRay(ray, out float distance)) { continue; }

                    nearestLength = Mathf.Min(nearestLength, Mathf.Max(distance, 0f));
                }
                pickRendererBuffer.Clear();
            }

            if (nearestLength == float.MaxValue) { return false; }

            worldPoint = ray.GetPoint(nearestLength);
            return true;
        }

        /// <summary>
        /// 光線が、ノーツの形(メッシュ)のうち「今プレビューに映っている部分」に当たる場所までの長さを求める
        ///
        /// 帯(ホールド)の形は、再生位置を過ぎた部分も含めて作られていて、過ぎた部分は描くときに切り落とされている。
        /// 切り落とされて見えない部分は視点のすぐそばを通っているので、そこも当たりに含めると、
        /// 何もない場所をクリックしても帯に当たってしまう。見えている部分だけを当たりにする。
        /// </summary>
        /// <param name="shadowRoot">地面に落ちる影の部品 (当たり判定に含めない)。無ければ null</param>
        /// <param name="hasReadableMesh">形を読み取れるメッシュが1つでもあったか (1つも無ければ、形では判定できない)</param>
        private bool TryGetVisibleMeshHitLength(Ray ray, Transform noteTransform, Transform shadowRoot, out float hitLength, out bool hasReadableMesh)
        {
            hitLength = float.MaxValue;
            hasReadableMesh = false;

            pickRay = ray;
            isVisibleHit ??= IsVisibleHit;

            noteTransform.GetComponentsInChildren(false, pickMeshFilterBuffer);
            foreach (MeshFilter meshFilter in pickMeshFilterBuffer)
            {
                Mesh mesh = meshFilter.sharedMesh;
                if (mesh == null || !mesh.isReadable) { continue; }
                // 表示範囲から外れた区間は、描画そのものが止められている
                if (!meshFilter.TryGetComponent(out Renderer meshRenderer) || !meshRenderer.enabled) { continue; }
                if (shadowRoot != null && meshFilter.transform.IsChildOf(shadowRoot)) { continue; }

                hasReadableMesh = true;
                if (!meshRenderer.bounds.IntersectRay(ray)) { continue; }

                // メッシュと同じ座標系に直した光線で調べる (長さの単位は元の光線と同じになる)
                Transform meshTransform = meshFilter.transform;
                Vector3 origin = meshTransform.InverseTransformPoint(ray.origin);
                Vector3 direction = meshTransform.InverseTransformVector(ray.direction);

                // 頂点と三角形は、使い回しのリストへ読み出す
                // (mesh.vertices / mesh.triangles は、呼ぶたびに配列を新しく作って返す。この判定はカーソルを動かすたびに
                //  帯の数だけ走るので、そのたびに配列を作ると、メモリの確保と片付けで動きが引っかかる)
                mesh.GetVertices(pickVertexBuffer);
                for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
                {
                    // 三角形でできていない部分 (線など) には当たらない
                    if (mesh.GetTopology(subMeshIndex) != MeshTopology.Triangles) { continue; }

                    mesh.GetTriangles(pickTriangleBuffer, subMeshIndex);
                    if (PreviewPickMath.TryRaycastTriangles(pickVertexBuffer, pickTriangleBuffer, origin, direction, isVisibleHit, out float length))
                    {
                        hitLength = Mathf.Min(hitLength, length);
                    }
                }
            }
            pickMeshFilterBuffer.Clear();
            pickVertexBuffer.Clear();
            pickTriangleBuffer.Clear();

            return hitLength < float.MaxValue;
        }

        /// <summary>
        /// ノーツが地面に落としている影の部品を返す (無ければ null)
        ///
        /// スペースのノーツは宙に浮いていて、その真下の地面に影を落とす。影もノーツの部品の1つだが、
        /// 本体から離れた場所に広く映るので、影をクリックしてもそのノーツを選んだことにはしない。
        /// (長い影は区間ごとに分けて描かれ、分けた部品は影の子になっているので、影とその子をまとめて外す)
        /// </summary>
        private static Transform GetShadowRoot(INoteData data)
        {
            // 譜面エディタ側にも同名のクラスがあるため、本編側のクラスは global:: を付けて指定する
            Renderer shadow = data switch
            {
                global::NoteData_SpaceHoldMesh spaceHoldMesh => spaceHoldMesh.MeshRendererAsset?.ShadowRenderer,
                global::NoteData_SpaceBreak spaceBreak => spaceBreak.MeshRendererAsset?.ShadowRenderer,
                _ => null,
            };

            return shadow != null ? shadow.transform : null;
        }

        /// <summary>
        /// 当たり判定中の光線(pickRay)の上の、指定した長さの場所が、今プレビューに映っている範囲にあるか
        /// </summary>
        private bool IsVisibleHit(float length)
        {
            if (noteParent == null) { return true; }

            // その場所が、軌道のどの位置(譜面上の距離)にあるか
            float curveRadius = optionHolder?.NoteCurveRadius.Value ?? 2000f;
            if (!PreviewTrackSurface.TryGetSection(pickRay.GetPoint(length), noteParent, curveRadius, out float distance, out _, out _)) { return true; }

            // 軌道は円なので、位置から求めた距離は一周ぶんの範囲に丸まっている
            // 再生位置にいちばん近い周回の距離に直してから比べる (長い譜面の後半でも正しく判定できるように)
            float currentDistance = CurrentDistance;
            distance = currentDistance + PreviewPickMath.WrapDistance(distance - currentDistance, 2f * Mathf.PI * curveRadius);

            return IsInVisibleRange(distance);
        }

        #endregion

        #region 差分の計算

        /// <summary>
        /// 1個ずつ独立して作れる種類の差分を取る
        /// 中身が同じノーツは残し、増えた分を生成、余った分を破棄する
        /// </summary>
        private void PlanIndependentType(
            NoteTypeState typeState,
            IEnumerable<INoteData> noteDataList,
            INotePositionCalculator positionCalculator,
            float noteSpeed,
            ref ApplyResult result)
        {
            CollectLiveNotesByKey(typeState);
            typeState.LiveNotes.Clear();

            foreach (INoteData data in noteDataList)
            {
                var key = PreviewNoteDataKeyBuilder.Build(data);

                // 同じ中身のノーツがすでにあれば、そのまま使う
                var kept = TakeLiveNote(key);
                if (kept != null)
                {
                    typeState.LiveNotes.Add(kept);
                    result.KeptCount++;
                    continue;
                }

                nearestQueue.Add(new SpawnTask
                {
                    TypeState = typeState,
                    Data = data,
                    Key = key,
                    Distance = positionCalculator.GetPosition(data.Timing) * noteSpeed,
                });
            }

            // 新しいデータに無かったノーツは不要
            result.DiscardCount += DiscardRemainingLiveNotes();
        }

        /// <summary>
        /// 生成結果がそれまでの生成に左右される種類の差分を取る
        /// 1個でも変わっていたら、ファクトリを初期化し直して、譜面順にすべて作り直す (本編と同じ結果にするため)
        /// 中身が同じノーツは、新しいものができるまで古いものを表示したままにする
        /// </summary>
        private void PlanOrderDependentType(
            NoteTypeState typeState,
            IEnumerable<INoteData> noteDataList,
            INotePositionCalculator positionCalculator,
            float noteSpeed,
            ref ApplyResult result)
        {
            // 新しいデータから生成の予定を作り、ノーツの並び全体を要約する
            plannedTasks.Clear();
            var sequenceHasher = PreviewKeyHasher.Create();
            foreach (INoteData data in noteDataList)
            {
                var key = PreviewNoteDataKeyBuilder.Build(data);
                sequenceHasher.Add(key);

                plannedTasks.Add(new SpawnTask
                {
                    TypeState = typeState,
                    Data = data,
                    Key = key,
                    Distance = positionCalculator.GetPosition(data.Timing) * noteSpeed,
                });
            }

            sequenceHasher.Add(plannedTasks.Count);
            var sequenceKey = sequenceHasher.ToKey();

            // 並びが前回作り終えたものと同じなら、何もしない
            if (typeState.HasCompletedSequence && typeState.CompletedSequenceKey.Equals(sequenceKey))
            {
                plannedTasks.Clear();
                result.KeptCount += typeState.LiveNotes.Count;
                return;
            }

            // 同じ並びを作り直している途中なら、最初からやり直さずに続きから再開する
            // (この種類に関係ない編集をするたびに作り直しが振り出しに戻るのを防ぐ)
            if (TryResumeInterruptedTasks(typeState, sequenceKey))
            {
                plannedTasks.Clear();
                result.KeptCount += typeState.LiveNotes.Count;
                return;
            }

            // すべて作り直す
            typeState.HasCompletedSequence = false;
            typeState.PendingSequenceKey = sequenceKey;
            typeState.PendingCount = plannedTasks.Count;
            InitializeFactory(typeState);

            // 今あるノーツは、新しいものができた時点で入れ替える「置き換え前」として残す
            // (古いノーツは LiveNotes に入れたままにして、途中で次の更新が来ても見失わないようにする)
            previousLiveNotes.Clear();
            previousLiveNotes.AddRange(typeState.LiveNotes);
            CollectLiveNotesByKey(typeState);
            typeState.LiveNotes.Clear();

            // 1. 中身が同じノーツ同士を対応させる (見た目が変わらないので、入れ替わるまで表示し続けてよい)
            for (int i = 0; i < plannedTasks.Count; i++)
            {
                var replaces = TakeLiveNote(plannedTasks[i].Key);
                if (replaces == null) { continue; }

                var task = plannedTasks[i];
                task.Replaces = replaces;
                plannedTasks[i] = task;
                typeState.LiveNotes.Add(replaces);
            }
            keyToLiveNote.Clear();

            // 2. 中身が変わったノーツは、残った古いノーツを前から順に割り当てる
            //    (編集したホールドが、新しい形で作り直されるまで画面から消えてしまうのを防ぐ。
            //     作り直されるまでの短い間は、編集前の形が表示される)
            int unmatchedTaskIndex = 0;
            foreach (var oldNote in previousLiveNotes)
            {
                oldNote.NextSameKey = null;
                if (oldNote.IsMatched) { continue; }

                while (unmatchedTaskIndex < plannedTasks.Count && plannedTasks[unmatchedTaskIndex].Replaces != null)
                {
                    unmatchedTaskIndex++;
                }

                // 割り当て先が無い = 数が減った分。すぐに消す
                if (unmatchedTaskIndex >= plannedTasks.Count)
                {
                    Discard(oldNote);
                    result.DiscardCount++;
                    continue;
                }

                var task = plannedTasks[unmatchedTaskIndex];
                task.Replaces = oldNote;
                plannedTasks[unmatchedTaskIndex] = task;
                typeState.LiveNotes.Add(oldNote);
            }
            previousLiveNotes.Clear();

            orderedQueue.AddRange(plannedTasks);

            // ノーツが1個も無くなった場合は、これで完了
            if (plannedTasks.Count == 0)
            {
                typeState.HasCompletedSequence = true;
                typeState.CompletedSequenceKey = sequenceKey;
            }

            plannedTasks.Clear();
        }

        /// <summary>
        /// 譜面順の作り直しが途中で、ノーツの並びが変わっていなければ、残りの生成待ちを引き継ぐ
        /// </summary>
        private bool TryResumeInterruptedTasks(NoteTypeState typeState, PreviewNoteDataKey sequenceKey)
        {
            if (typeState.HasCompletedSequence || typeState.PendingCount <= 0) { return false; }
            if (!typeState.PendingSequenceKey.Equals(sequenceKey)) { return false; }

            int resumeStartIndex = orderedQueue.Count;
            foreach (var task in interruptedOrderedTasks)
            {
                if (task.TypeState == typeState) { orderedQueue.Add(task); }
            }

            // 残りの数が合わないときは、引き継がずに作り直す
            int resumedCount = orderedQueue.Count - resumeStartIndex;
            if (resumedCount != typeState.PendingCount)
            {
                orderedQueue.RemoveRange(resumeStartIndex, resumedCount);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 今あるノーツを、中身のキーで引けるようにまとめる
        /// </summary>
        private void CollectLiveNotesByKey(NoteTypeState typeState)
        {
            keyToLiveNote.Clear();

            foreach (var note in typeState.LiveNotes)
            {
                // 同じ中身のノーツが複数あるときは数珠つなぎにする
                keyToLiveNote.TryGetValue(note.Key, out var sameKeyNote);
                note.IsMatched = false;
                note.NextSameKey = sameKeyNote;
                keyToLiveNote[note.Key] = note;
            }
        }

        /// <summary>
        /// 指定した中身のノーツを1個取り出す。無ければ null
        /// </summary>
        private SpawnedNote TakeLiveNote(PreviewNoteDataKey key)
        {
            if (!keyToLiveNote.TryGetValue(key, out var note)) { return null; }

            if (note.NextSameKey != null) { keyToLiveNote[key] = note.NextSameKey; }
            else { keyToLiveNote.Remove(key); }

            note.NextSameKey = null;
            note.IsMatched = true;
            return note;
        }

        /// <summary>
        /// 取り出されずに残ったノーツを破棄の予定に入れる
        /// </summary>
        private int DiscardRemainingLiveNotes()
        {
            int count = 0;

            foreach (var head in keyToLiveNote.Values)
            {
                var note = head;
                while (note != null)
                {
                    var next = note.NextSameKey;
                    note.NextSameKey = null;
                    Discard(note);
                    count++;
                    note = next;
                }
            }

            keyToLiveNote.Clear();
            return count;
        }

        /// <summary>
        /// ノーツを破棄の予定に入れる (非表示にするのは呼び出し側でまとめて行う)
        /// </summary>
        private void Discard(SpawnedNote note)
        {
            if (note.Target != null) { discardedTargets.Add(note.Target); }
            if (note.Component != null) { EnqueueDestroy(note.Component.gameObject, note.HasGeneratedMeshes); }
        }

        /// <summary>
        /// 今あるノーツをすべて非表示にして、破棄の予定に入れる
        /// </summary>
        private int DiscardAllLiveNotes()
        {
            int count = 0;
            trackedNoteObjects.Clear();

            foreach (var typeState in typeStates)
            {
                foreach (var note in typeState.LiveNotes)
                {
                    if (note.Component != null)
                    {
                        EnqueueDestroy(note.Component.gameObject, note.HasGeneratedMeshes);
                        trackedNoteObjects.Add(note.Component.gameObject);
                    }
                    count++;
                }

                typeState.LiveNotes.Clear();
                typeState.HasCompletedSequence = false;
                typeState.PendingCount = 0;
            }

            // 生成に失敗して作りかけのまま残ったオブジェクトなど、把握していないものも片付ける
            // (すでに破棄待ちのものが重ねて入ることがあるが、破棄済みなら何もしないので問題ない)
            if (noteParent != null)
            {
                for (int i = 0; i < noteParent.childCount; i++)
                {
                    GameObject child = noteParent.GetChild(i).gameObject;
                    if (trackedNoteObjects.Contains(child)) { continue; }

                    child.SetActive(false);
                    EnqueueDestroy(child, false);
                }
            }

            trackedNoteObjects.Clear();
            noteVisibilityController?.Clear();
            return count;
        }

        /// <summary>
        /// 位置の決まり方を要約する
        /// </summary>
        private static PreviewNoteDataKey BuildLayoutKey(PositionGraph positionGraph, float noteSpeed, float curveRadius)
        {
            var hasher = PreviewKeyHasher.Create();
            hasher.Add(noteSpeed);
            hasher.Add(curveRadius);

            // 速度倍率が切り替わる点だけを使う
            // (区間は分線ごとに1つあるので、数まで含めると、譜面を延長しただけで全部作り直しになってしまう)
            var segments = positionGraph.Segments;
            for (int i = 0; i < segments.Count; i++)
            {
                if (i > 0 && segments[i].Slope == segments[i - 1].Slope) { continue; }

                hasher.Add(segments[i].StartTime);
                hasher.Add(segments[i].Slope);
            }

            return hasher.ToKey();
        }

        #endregion

        #region 生成と破棄 (少しずつ)

        private void Update()
        {
            ProcessDestroyQueue();
            ProcessSpawnQueues();
        }

        /// <summary>
        /// 破棄待ちのノーツを少しずつ破棄する (すでに非表示なので急がない)
        /// </summary>
        private void ProcessDestroyQueue()
        {
            int count = 0;
            while (destroyQueue.Count > 0 && count < maxDestroyCountPerFrame)
            {
                var task = destroyQueue.Dequeue();
                DestroyNoteObject(task.NoteObject, task.HasGeneratedMeshes);
                count++;
            }
        }

        private void EnqueueDestroy(GameObject noteObject, bool hasGeneratedMeshes)
        {
            destroyQueue.Enqueue(new DestroyTask { NoteObject = noteObject, HasGeneratedMeshes = hasGeneratedMeshes });
        }

        /// <summary>
        /// ノーツのオブジェクトを破棄する
        /// </summary>
        /// <param name="hasGeneratedMeshes">
        /// true のとき、ファクトリが実行時に作ったメッシュも破棄する
        /// (オブジェクトを破棄してもメッシュは残り続けるので、作り直しのたびにメモリが増えてしまう)
        /// </param>
        private void DestroyNoteObject(GameObject noteObject, bool hasGeneratedMeshes)
        {
            if (noteObject == null) { return; }

            if (hasGeneratedMeshes)
            {
                noteObject.GetComponentsInChildren(true, meshFilterBuffer);
                foreach (MeshFilter meshFilter in meshFilterBuffer)
                {
                    Mesh mesh = meshFilter.sharedMesh;

                    // 実行時に作られたものだけを破棄する (プロジェクト内のアセットは番号が正、実行時に作ったものは負)
                    if (mesh != null && mesh.GetInstanceID() < 0) { Destroy(mesh); }
                }
                meshFilterBuffer.Clear();
            }

            Destroy(noteObject);
        }

        /// <summary>
        /// 生成待ちのノーツを、1フレームに使ってよい時間のぶんだけ生成する
        /// </summary>
        private void ProcessSpawnQueues()
        {
            if (!IsGenerating) { return; }

            ResortNearestQueueIfNeeded();

            stopwatch.Restart();
            bool isProcessedAny = false;

            while (IsGenerating)
            {
                // 最低1個は進める (1個の生成が長くても止まらないように)
                if (isProcessedAny && stopwatch.Elapsed.TotalMilliseconds >= frameBudgetMilliseconds) { break; }

                // 「近い順」と「譜面順」を交互に進める
                bool hasNearest = nearestIndex < nearestQueue.Count;
                bool hasOrdered = orderedIndex < orderedQueue.Count;
                bool useOrdered = hasOrdered && (!hasNearest || isOrderedTurn);
                isOrderedTurn = !isOrderedTurn;

                if (useOrdered) { Spawn(orderedQueue[orderedIndex++]); }
                else { Spawn(nearestQueue[nearestIndex++]); }

                isProcessedAny = true;
            }

            if (!IsGenerating) { ClearSpawnQueues(); }
        }

        /// <summary>
        /// ノーツを1個生成する
        /// </summary>
        private void Spawn(SpawnTask task)
        {
            SpawnOne(task);
        }

        /// <summary>
        /// 作る途中で失敗したときに、シーンのいちばん上に残った作りかけのものを消す
        /// </summary>
        private static void DestroyLeftovers(UnityEngine.SceneManagement.Scene scene, int rootCountBefore)
        {
            if (rootCountBefore < 0 || !scene.IsValid() || scene.rootCount <= rootCountBefore) { return; }

            var roots = scene.GetRootGameObjects();
            for (int i = rootCountBefore; i < roots.Length; i++)
            {
                if (roots[i] != null) { Destroy(roots[i]); }
            }
        }

        private void SpawnOne(SpawnTask task)
        {
            var typeState = task.TypeState;
            Component spawnedComponent = null;

            // 作る途中で失敗すると、作りかけのもの (ノーツの入れ物や、その中の絵) が、シーンのいちばん上に残ってしまう。
            // 新しく作られたものは、いちばん上の並びの最後に入るので、作る前の数を覚えておき、失敗したらそれより後ろを消す
            var activeScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            int rootCountBefore = activeScene.IsValid() ? activeScene.rootCount : -1;

            try
            {
                spawnArguments[0] = task.Data;
                spawnArguments[1] = chartData.PositionGraph;
                spawnedComponent = typeState.SpawnMethod.Invoke(typeState.Factory, spawnArguments) as Component;
            }
            catch (Exception ex)
            {
                // リフレクション経由の例外は、中身のほうを表示する
                var cause = ex is TargetInvocationException && ex.InnerException != null ? ex.InnerException : ex;
                Debug.LogWarning($"[ChartPreviewGenerator] Spawn failed ({typeState.NoteType}): {cause.Message}");
                DestroyLeftovers(activeScene, rootCountBefore);
            }

            if (spawnedComponent != null)
            {
                if (noteParent != null) { spawnedComponent.transform.SetParent(noteParent, true); }

                var note = new SpawnedNote
                {
                    Component = spawnedComponent,
                    Target = spawnedComponent as INoteVisibilityTarget,
                    Data = task.Data,
                    Key = task.Key,
                    HasGeneratedMeshes = typeState.IsOrderDependent,
                };

                typeState.LiveNotes.Add(note);
                noteVisibilityController?.Add(spawnedComponent);
            }

            // 置き換え前のノーツを、新しいものと同じフレームで消す (ちらつかせない)
            if (task.Replaces != null)
            {
                typeState.LiveNotes.Remove(task.Replaces);
                if (task.Replaces.Component != null)
                {
                    noteVisibilityController?.Remove(task.Replaces.Component);
                    DestroyNoteObject(task.Replaces.Component.gameObject, task.Replaces.HasGeneratedMeshes);
                }
            }

            // 譜面順に作り直している種類は、最後の1個まで作れたら完了
            if (typeState.IsOrderDependent && typeState.PendingCount > 0)
            {
                typeState.PendingCount--;
                if (typeState.PendingCount == 0)
                {
                    typeState.HasCompletedSequence = true;
                    typeState.CompletedSequenceKey = typeState.PendingSequenceKey;
                }
            }
        }

        private void ClearSpawnQueues()
        {
            nearestQueue.Clear();
            nearestIndex = 0;
            orderedQueue.Clear();
            orderedIndex = 0;
        }

        /// <summary>
        /// 「近い順」の生成待ちを、今の再生位置を基準に並べる
        /// </summary>
        private void SortNearestQueue()
        {
            // 処理済みの分を取り除く
            if (nearestIndex > 0)
            {
                nearestQueue.RemoveRange(0, nearestIndex);
                nearestIndex = 0;
            }

            float currentDistance = noteVisibilityController != null ? noteVisibilityController.CurrentDistance : 0f;
            sortedAtDistance = currentDistance;
            if (nearestQueue.Count <= 1) { return; }

            float behindLimit = currentDistance - priorityBehindDistance;
            nearestQueue.Sort((a, b) => CompareSpawnPriority(a.Distance, b.Distance, behindLimit));
        }

        /// <summary>
        /// 生成する順番の比較 (先に生成するほうが小さい)
        /// これから流れてくるノーツを再生位置に近い順に作り、通過済みのノーツはその後に回す
        /// </summary>
        private static int CompareSpawnPriority(float distanceA, float distanceB, float behindLimit)
        {
            bool isBehindA = distanceA < behindLimit;
            bool isBehindB = distanceB < behindLimit;

            // これから流れてくるノーツが先
            if (isBehindA != isBehindB) { return isBehindA ? 1 : -1; }

            // どちらも再生位置に近い順
            // (これから流れてくるノーツは距離が小さいほど近く、通過済みのノーツは距離が大きいほど近い)
            return isBehindA ? distanceB.CompareTo(distanceA) : distanceA.CompareTo(distanceB);
        }

        /// <summary>
        /// 生成の途中で再生位置が大きく動いたら、生成する順番を並べ直す
        /// </summary>
        private void ResortNearestQueueIfNeeded()
        {
            if (nearestIndex >= nearestQueue.Count) { return; }
            if (noteVisibilityController == null) { return; }
            if (Mathf.Abs(noteVisibilityController.CurrentDistance - sortedAtDistance) < resortDistance) { return; }

            SortNearestQueue();
        }

        #endregion

        #region ファクトリの初期化

        private void InitializeFactories()
        {
            if (isFactoriesInitialized) { return; }
            isFactoriesInitialized = true;

            factoryInitializingData = new NoteFactoryInitializingData
            {
                NoteParent = noteParent,
                Difficulty = previewDifficulty,
                JudgementSettingsCatalog = judgementSettingsCatalog,
                OptionHolder = optionHolder ?? null,
                Timer = timer.Value,
                JudgementRecorder = null
            };

            if (noteFactories == null) { return; }
            foreach (var binding in noteFactories)
            {
                if (binding == null || binding.Factory == null) { continue; }

                Type factoryType = binding.Factory.GetType();
                var typeState = new NoteTypeState
                {
                    NoteType = binding.NoteType,
                    Factory = binding.Factory,
                    InitializeMethod = factoryType.GetMethod("Initialize", new[] { typeof(NoteFactoryInitializingData) }),
                    SpawnMethod = FindSpawnMethod(factoryType),
                    IsOrderDependent = binding.Factory is IOrderDependentNoteFactory,
                };

                InitializeFactory(typeState);

                if (typeState.SpawnMethod == null)
                {
                    Debug.LogWarning($"[ChartPreviewGenerator] Spawn method not found: {factoryType.Name}");
                    continue;
                }

                typeStates.Add(typeState);
            }
        }

        private void InitializeFactory(NoteTypeState typeState)
        {
            if (typeState.InitializeMethod == null)
            {
                Debug.LogWarning($"[ChartPreviewGenerator] Initialize method not found: {typeState.Factory.GetType().Name}");
                return;
            }

            typeState.InitializeMethod.Invoke(typeState.Factory, new object[] { factoryInitializingData });
        }

        private static MethodInfo FindSpawnMethod(Type factoryType)
        {
            foreach (MethodInfo m in factoryType.GetMethods())
            {
                if (m.Name != "Spawn") { continue; }
                ParameterInfo[] p = m.GetParameters();
                if (p.Length != 2) { continue; }
                if (typeof(INotePositionCalculator).IsAssignableFrom(p[1].ParameterType))
                {
                    return m;
                }
            }

            return null;
        }

        #endregion
    }
}

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
        readonly List<(float Distance, INoteData Data)> pickCandidates = new List<(float, INoteData)>();
        readonly object[] spawnArguments = new object[2];
        readonly List<Renderer> pickRendererBuffer = new List<Renderer>();
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
        /// 光線が当たるノーツのデータを、調べる順に並べて返す (プレビュー上のクリック判定用)
        /// 先に単発のノーツを手前から順に、その後にホールドの帯を手前から順に並べる
        /// (帯は当たり判定の範囲が広く、近くの単発ノーツより先に当たってしまうので後回しにする)
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

                    // そのノーツの描画範囲のうち、光線がいちばん手前で当たる距離
                    float nearestDistance = float.MaxValue;
                    noteTransform.GetComponentsInChildren(false, pickRendererBuffer);
                    foreach (Renderer noteRenderer in pickRendererBuffer)
                    {
                        if (!noteRenderer.enabled) { continue; }
                        if (!noteRenderer.bounds.IntersectRay(ray, out float distance)) { continue; }

                        nearestDistance = Mathf.Min(nearestDistance, distance);
                    }

                    if (nearestDistance < float.MaxValue) { pickCandidates.Add((nearestDistance, note.Data)); }
                }
            }

            pickRendererBuffer.Clear();

            pickCandidates.Sort((a, b) => a.Distance.CompareTo(b.Distance));
            foreach (var candidate in pickCandidates)
            {
                results.Add(candidate.Data);
            }
            pickCandidates.Clear();
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
            var typeState = task.TypeState;
            Component spawnedComponent = null;

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

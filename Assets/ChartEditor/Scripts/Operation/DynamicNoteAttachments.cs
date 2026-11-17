using System;
using System.Collections.Generic;
using System.Text;
using UniRx;
using UnityEngine;
using VContainer.Unity;
using static UndoRedo.History;

namespace ChartEditor
{
    /// <summary>
    /// ダイナミックノーツを、地上のノーツに載せる (録音ではなく、2026-11-17 の指示)
    ///
    /// ・ダイナミックノーツは、単独では置けない。地上のノーツ (HoldMeshRelay を除く) の上にだけ置ける。1つのノーツに1つまで
    ///   (違う向きのものを置くと、向きが置き換わる。幅は、載せたノーツと同じ)
    /// ・ダイナミックノーツと下のノーツは、別々に選べる。ただし扱いは1つのノーツと同じ:
    ///   どちらを動かしても・幅を変えても一緒に変わる、下のノーツを消すと一緒に消える、ロック・コピー・反転は両方に働く
    ///   (ダイナミックノーツだけを選んで消すと、ダイナミックノーツだけが外れる)
    ///   一緒に動かすのは、動かす操作の側 (WithPartners で相手も対象にする。元に戻す履歴にも、両方が入る)。
    ///   ここでは動かさない: 片方だけが動いたときは、組を外す (動いたダイナミックノーツは、ルールに合わないものになる)
    /// ・同じ幅のノーツが2つ以上重なっている所には載せない (読み込み直したときに、どちらに載っていたか決められないので)
    /// ・載っているノーツの種類は変えられる。ただし HoldMeshRelay にはできない (ログで知らせる)
    /// ・譜面ファイルの形は変えない。どのノーツに載っているかは、編集している間だけ覚えておき、
    ///   読み込んだときに「同じ分線・同じ幅」で決め直す
    /// ・ルールに合わないダイナミックノーツ (古い譜面の、下にノーツが無い・幅が合わない・同じ幅のノーツが2つ以上重なっている・
    ///   HoldMeshRelay の上にある・1つのノーツに2つ目) は、赤い縁取りを付け、レギュレーション違反として知らせる (直すまで保存できない)。
    ///   今までどおり単独で動かせる・幅を変えられる・消せるが、ロック・コピー・種類の変更はできない。
    ///   動かして地上のノーツにぴったり重ねると、そのノーツに載る
    /// </summary>
    public class DynamicNoteAttachments : ILateTickable, IDisposable
    {
        static DynamicNoteAttachments instance;

        // ルールに合わないダイナミックノーツがあるとき、載せられるようになったか調べ直す間隔 (秒)
        const float RECHECK_INTERVAL_SECONDS = 0.25f;

        // ルールに合わないダイナミックノーツの縁取り (重なっているノーツの警告と同じ、点滅する赤)
        static readonly ColorSetting violationColor = new ColorSetting { Color = new Color(1f, 0f, 0f, 1f), IsBlinking = true };

        readonly IChartEditorDataGetter dataGetter;
        readonly INotesDataGetter notesGetter;
        readonly DynamicNoteLinker linker = new DynamicNoteLinker();
        readonly CompositeDisposable disposables = new CompositeDisposable();

        readonly List<IDeployableNoteData> liveNotes = new List<IDeployableNoteData>();
        readonly Dictionary<IDeployableNoteData, NoteObject> noteObjects = new Dictionary<IDeployableNoteData, NoteObject>();
        // 赤い縁取りを付けたノーツと、付けたときの絵
        readonly Dictionary<IDeployableNoteData, NoteObject> marked = new Dictionary<IDeployableNoteData, NoteObject>();
        readonly List<IDeployableNoteData> markBuffer = new List<IDeployableNoteData>();

        ChartData boundChart;
        bool isLoadingSeen;
        bool isDirty = true;
        bool isReportPending;
        float nextRecheckTime;

        public DynamicNoteAttachments(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
            instance = this;

            // 譜面にノーツが増えた・減ったら、載っているノーツを決め直す
            notesGetter.DataToNoteObject.ObserveAdd().Subscribe(_ => isDirty = true).AddTo(disposables);
            // 譜面から無くなったノーツの組は、すぐ外す (同じ操作の中で、ホールドの終点の種類などが、消えたダイナミックノーツを見ないように)
            notesGetter.DataToNoteObject.ObserveRemove().Subscribe(removed =>
            {
                isDirty = true;
                linker.ForgetRemoved(removed.Value?.Data);
            }).AddTo(disposables);
            notesGetter.DataToNoteObject.ObserveReset().Subscribe(_ => isDirty = true).AddTo(disposables);
        }

        void IDisposable.Dispose()
        {
            disposables.Dispose();
            if (instance == this) { instance = null; }
        }

        #region 外から使うもの

        /// <summary>
        /// そのノーツに載っているダイナミックノーツ (無ければ null)
        /// </summary>
        public static IDeployableNoteData GetDynamicOn(IDeployableNoteData host)
        {
            return instance != null ? instance.linker.GetDynamicOn(host) : null;
        }

        /// <summary>
        /// ダイナミックノーツが載っているノーツ (載っていなければ null)
        /// </summary>
        public static IDeployableNoteData GetHostOf(IDeployableNoteData dynamic)
        {
            return instance != null ? instance.linker.GetHostOf(dynamic) : null;
        }

        /// <summary>
        /// 一緒に扱う相手 (ダイナミックノーツなら下のノーツ、下のノーツなら載っているダイナミックノーツ。無ければ null)
        /// </summary>
        public static IDeployableNoteData GetPartner(IDeployableNoteData note)
        {
            if (instance == null || note == null) { return null; }
            return instance.linker.GetHostOf(note) ?? instance.linker.GetDynamicOn(note);
        }

        /// <summary>
        /// ルールに合わない (どのノーツにも載っていない) ダイナミックノーツか
        /// </summary>
        public static bool IsViolation(IDeployableNoteData note)
        {
            return instance != null && instance.linker.IsViolation(note);
        }

        /// <summary>
        /// ノーツの一覧に、一緒に扱う相手を足したもの (順番は元の一覧のまま、相手は後ろに足す。同じノーツは1回だけ)
        /// </summary>
        /// <param name="includeDynamics">下のノーツに載っているダイナミックノーツを足すか</param>
        /// <param name="includeHosts">ダイナミックノーツの下のノーツを足すか</param>
        public static List<IDeployableNoteData> WithPartners(IEnumerable<IDeployableNoteData> notes, bool includeDynamics = true, bool includeHosts = true)
        {
            var result = new List<IDeployableNoteData>();
            if (notes == null) { return result; }

            var seen = new HashSet<IDeployableNoteData>();
            foreach (var note in notes)
            {
                if (note != null && seen.Add(note)) { result.Add(note); }
            }

            if (instance == null) { return result; }

            int count = result.Count;
            for (int i = 0; i < count; i++)
            {
                var note = result[i];
                IDeployableNoteData partner = null;
                if (includeHosts) { partner = instance.linker.GetHostOf(note); }
                if (partner == null && includeDynamics) { partner = instance.linker.GetDynamicOn(note); }
                if (partner != null && seen.Add(partner)) { result.Add(partner); }
            }

            return result;
        }

        /// <summary>
        /// ダイナミックノーツを、どのノーツに載せるつもりかを覚えておく (置くとき・貼り付けるときに、譜面に足す前に呼ぶ)
        /// 同じ幅のノーツが重なっている場所でも、そのノーツに載る
        /// </summary>
        public static void Remember(IDeployableNoteData host, IDeployableNoteData dynamic)
        {
            instance?.linker.Remember(host, dynamic);
        }

        /// <summary>
        /// ダイナミックノーツを置く道具を使っているか (グラウンドのレイヤーで、配置するノーツにダイナミックノーツを選んでいる)
        /// </summary>
        public static bool IsPlacingDynamic(IChartEditorDataGetter dataGetter)
        {
            return dataGetter != null &&
                   !DeploymentNoteChoice.IsNone &&
                   dataGetter.EditNoteType.Value == EditNoteType.Ground &&
                   DynamicNoteRules.IsDynamic(dataGetter.DeploymentNoteType.Value);
        }

        /// <summary>
        /// カーソルの下のノーツから、ダイナミックノーツを載せるノーツを決める
        /// (ダイナミックノーツの上なら、その下のノーツ。載せられないときは null と、その理由)
        /// </summary>
        public static IDeployableNoteData ResolveHost(IDeployableNoteData note, out string refusal)
        {
            refusal = null;
            if (note == null) { return null; }

            if (DynamicNoteRules.IsDynamic(note.NoteType))
            {
                var host = GetHostOf(note);
                if (host == null)
                {
                    refusal = "ルールに合わないダイナミックノーツの上には、置けません";
                    return null;
                }
                note = host;
            }

            if (note.NoteType == DeploymentNoteType.HoldMeshRelay)
            {
                refusal = "HoldMeshRelay には、ダイナミックノーツを置けません";
                return null;
            }
            if (!DynamicNoteRules.CanCarry(note.NoteType))
            {
                refusal = "ダイナミックノーツは、地上のノーツの上にだけ置けます";
                return null;
            }
            if (NoteLock.IsLocked(note))
            {
                refusal = "ロックしたノーツには、ダイナミックノーツを置けません";
                return null;
            }
            // 同じ幅のノーツが重なっている所 (録音 20261118_005903: 置けるようにする): 押したノーツにもう載っていれば、重なっている
            // ノーツのうち、まだ何も載っていないものに載せる (どれも載っていれば、押したノーツの向きを置き換える)
            if (instance != null && GetDynamicOn(note) != null)
            {
                var free = instance.linker.FindFreeCarrierAt(note.Address);
                if (free != null && !NoteLock.IsLocked(free)) { note = free; }
            }

            return note;
        }

        /// <summary>
        /// ダイナミックノーツを置く道具のとき、カーソルの下にあったら「置く」場所にするノーツか
        /// (地上のノーツと、ノーツに載っているダイナミックノーツ。ルールに合わないダイナミックノーツは、今までどおり動かせるように含めない)
        /// </summary>
        public static bool IsPlacementTarget(IDeployableNoteData note)
        {
            if (note == null) { return false; }
            if (DynamicNoteRules.IsDynamic(note.NoteType)) { return GetHostOf(note) != null; }
            return DynamicNoteRules.IsGroundNote(note.NoteType);
        }

        /// <summary>
        /// ダイナミックノーツを、ノーツに載せる (すでに違う向きのものが載っていれば、置き換える。元に戻すのは1回ぶん)
        /// </summary>
        /// <returns>載せた (置き換えた) とき true</returns>
        public static bool Attach(ChartData chartData, IDeployableNoteData host, IDeployableNoteData dynamic)
        {
            if (chartData == null || host == null || dynamic == null) { return false; }

            var existing = GetDynamicOn(host);
            if (existing != null && existing.NoteType == dynamic.NoteType)
            {
                Debug.Log("【配置】このノーツには、同じ向きのダイナミックノーツがもう載っています");
                return false;
            }

            // 幅と場所は、載せるノーツと同じ
            dynamic.SetAddress(new AddressWithinRange(host.Address));
            Remember(host, dynamic);

            // 判定ありの終点に載せるときは、判定なしの終点にする (元に戻すと、判定ありに戻る。録音 20261118_010356)
            var judgedEnd = host.NoteType == DeploymentNoteType.HoldEnd ? host as ITypeChangableNoteData : null;

            Record(() =>
            {
                if (existing != null) { chartData.RemoveNote(existing, false); }
                chartData.AddNote(dynamic);
                judgedEnd?.SetNoteType(DeploymentNoteType.HoldEndUnjudge);
            },
            () =>
            {
                judgedEnd?.SetNoteType(DeploymentNoteType.HoldEnd);
                chartData.RemoveNote(dynamic, false);
                if (existing != null) { chartData.AddNote(existing); }
            });
            if (judgedEnd != null) { Debug.Log("【配置】ダイナミックノーツを載せたので、ホールドの終点を判定なしにしました"); }

            Debug.Log(existing != null
                ? $"【配置】ダイナミックノーツの向きを置き換えました:\n{dynamic.Address}"
                : $"【配置】ダイナミックノーツを載せました:\n{dynamic.Address}");
            return true;
        }

        /// <summary>
        /// ノーツの種類を変えてよいか (ダイナミックノーツが載っているノーツは HoldMeshRelay にできない。
        /// ルールに合わないダイナミックノーツは、種類を変えられない)
        /// </summary>
        public static bool CanChangeType(IDeployableNoteData note, DeploymentNoteType next, out string refusal)
        {
            refusal = null;
            if (note == null) { return true; }

            if (next == DeploymentNoteType.HoldMeshRelay && GetDynamicOn(note) != null)
            {
                refusal = "ダイナミックノーツが載っているので、HoldMeshRelay にはできません (先にダイナミックノーツを消してください)";
                return false;
            }
            if (next == DeploymentNoteType.HoldEnd && GetDynamicOn(note) != null)
            {
                refusal = "ダイナミックノーツが載っている終点は、判定なしのままにします (判定ありにするには、先にダイナミックノーツを消してください)";
                return false;
            }
            if (IsViolation(note))
            {
                refusal = "ルールに合わないダイナミックノーツは、種類を変えられません (地上のノーツにぴったり載せ直すか、消してください)";
                return false;
            }
            return true;
        }

        /// <summary>
        /// 操作を受け付けなかったことを、ログに出す (同じ理由のものは1行にまとめる)
        /// </summary>
        public static void LogRefused(int count, string refusal)
        {
            if (count <= 0 || string.IsNullOrEmpty(refusal)) { return; }
            Debug.Log(count > 1 ? $"【ダイナミック】{refusal} ({count} 個)" : $"【ダイナミック】{refusal}");
        }

        #endregion

        void ILateTickable.LateTick()
        {
            // 読み込み中は待つ (ノーツが途中までしか無いので、実際には無い違反が出てしまう)
            if (dataGetter.IsLoadingChart.Value)
            {
                isLoadingSeen = true;
                return;
            }

            // 別の譜面を開いた・読み込み直した: 覚えていることを捨てて、決め直す
            var chart = dataGetter.ChartData.Value;
            if (!ReferenceEquals(chart, boundChart) || isLoadingSeen)
            {
                boundChart = chart;
                isLoadingSeen = false;
                linker.Clear();
                UnmarkAll();
                isDirty = true;
                isReportPending = true;
            }
            if (chart == null) { return; }

            // 片方だけが動いた組や、ルールに合わないダイナミックノーツが動いたことを見つける (決め直しは、下で)
            if (linker.Sync()) { isDirty = true; }

            // ドラッグの途中 (ノーツの移動・幅、ホールドの曲線の印など) は決め直さない。離してから決める
            // (途中で、通りかかったノーツに載ってしまわないように)
            if (Input.GetMouseButton(0)) { return; }

            if (isDirty || (linker.ViolationCount > 0 && Time.unscaledTime >= nextRecheckTime))
            {
                Reconcile();
            }
        }

        private void Reconcile()
        {
            isDirty = false;
            nextRecheckTime = Time.unscaledTime + RECHECK_INTERVAL_SECONDS;

            liveNotes.Clear();
            noteObjects.Clear();
            var pairs = notesGetter.DataToNoteObject;
            for (int i = 0; i < pairs.Count; i++)
            {
                var pair = pairs[i];
                if (pair == null || pair.Data == null || pair.Object == null) { continue; }
                liveNotes.Add(pair.Data);
                noteObjects[pair.Data] = pair.Object;
            }

            linker.Reconcile(liveNotes);
            int converted = MakeCarriedEndsUnjudged();
            UpdateMarks();

            if (isReportPending)
            {
                isReportPending = false;
                if (converted > 0) { Debug.Log($"【ダイナミック】ダイナミックノーツが載っているホールドの終点を、判定なしにしました ({converted} 個)。保存すると、譜面にも残ります"); }
                Report();
            }
            else if (converted > 0)
            {
                Debug.Log(converted > 1 ? $"【ダイナミック】ダイナミックノーツが載ったホールドの終点を、判定なしにしました ({converted} 個)"
                                        : "【ダイナミック】ダイナミックノーツが載ったホールドの終点を、判定なしにしました");
            }
        }

        /// <summary>
        /// ダイナミックノーツが載っている、判定ありの終点を、判定なしの終点にする (録音 20261118_010356: 終点に載せたら、必ず判定を消す)
        /// (置いたときは、置く操作の中で変えている。ここは、読み込んだ譜面・動かして載ったとき・中継点が終点になったときの分)
        /// </summary>
        /// <returns>変えた数</returns>
        private int MakeCarriedEndsUnjudged()
        {
            int count = 0;
            foreach (var note in liveNotes)
            {
                if (note.NoteType != DeploymentNoteType.HoldEnd || note is not ITypeChangableNoteData typed) { continue; }
                if (linker.GetDynamicOn(note) == null) { continue; }

                typed.SetNoteType(DeploymentNoteType.HoldEndUnjudge);
                count++;
            }
            return count;
        }

        /// <summary>
        /// ルールに合わないダイナミックノーツに、赤い縁取りを付ける・外す
        /// </summary>
        private void UpdateMarks()
        {
            markBuffer.Clear();
            foreach (var pair in marked)
            {
                bool keep = linker.IsViolation(pair.Key) && noteObjects.TryGetValue(pair.Key, out var current) && current == pair.Value;
                if (!keep) { markBuffer.Add(pair.Key); }
            }
            foreach (var note in markBuffer)
            {
                Unmark(marked[note]);
                marked.Remove(note);
            }

            foreach (var note in linker.Violations)
            {
                if (marked.ContainsKey(note)) { continue; }
                if (!noteObjects.TryGetValue(note, out var noteObject) || noteObject == null) { continue; }

                noteObject.OutlineColors.Add(violationColor);
                noteObject.SetWarning(NoteWarningType.LooseDynamic, true);
                marked[note] = noteObject;
            }
        }

        private void UnmarkAll()
        {
            foreach (var noteObject in marked.Values) { Unmark(noteObject); }
            marked.Clear();
        }

        private static void Unmark(NoteObject noteObject)
        {
            if (noteObject == null) { return; }
            noteObject.OutlineColors.Remove(violationColor);
            noteObject.SetWarning(NoteWarningType.LooseDynamic, false);
        }

        /// <summary>
        /// 読み込んだ譜面に、ルールに合わないダイナミックノーツがあれば、直すようにログで知らせる
        /// (場所の一覧は、レギュレーション違反のお知らせ (RegulationWarningNotifier) に出る)
        /// </summary>
        private void Report()
        {
            if (linker.ViolationCount == 0) { return; }

            var counts = new Dictionary<DynamicViolationReason, int>();
            foreach (var note in linker.Violations)
            {
                var reason = linker.GetViolationReason(note);
                counts[reason] = counts.TryGetValue(reason, out int count) ? count + 1 : 1;
            }

            var builder = new StringBuilder();
            builder.Append("【ダイナミック】この譜面には、今のルールに合わないダイナミックノーツが ").Append(linker.ViolationCount).Append(" 個あります。")
                .Append("\n赤い縁取りのダイナミックノーツを、地上のノーツ (HoldMeshRelay 以外) にぴったり載せ直すか、消してください。直すまで保存できません。");
            foreach (DynamicViolationReason reason in Enum.GetValues(typeof(DynamicViolationReason)))
            {
                if (!counts.TryGetValue(reason, out int count)) { continue; }
                builder.Append("\n ・").Append(DynamicNoteRules.GetReasonText(reason)).Append(": ").Append(count).Append(" 個");
            }

            Debug.LogWarning(builder.ToString());
        }
    }

    /// <summary>
    /// ダイナミックノーツを載せるときの決まり
    /// </summary>
    public static class DynamicNoteRules
    {
        /// <summary>
        /// ダイナミックノーツか
        /// </summary>
        public static bool IsDynamic(DeploymentNoteType type)
        {
            return type == DeploymentNoteType.DynamicGroundUpward ||
                   type == DeploymentNoteType.DynamicGroundDownward ||
                   type == DeploymentNoteType.DynamicGroundRightward ||
                   type == DeploymentNoteType.DynamicGroundLeftward;
        }

        /// <summary>
        /// ダイナミックノーツを載せられる種類か (地上のノーツのうち、HoldMeshRelay 以外)
        /// </summary>
        public static bool CanCarry(DeploymentNoteType type)
        {
            switch (type)
            {
                case DeploymentNoteType.Touch:
                case DeploymentNoteType.DivineTouch:
                case DeploymentNoteType.HoldStart:
                case DeploymentNoteType.DivineHoldStart:
                case DeploymentNoteType.HoldRelay:
                case DeploymentNoteType.HoldEnd:
                case DeploymentNoteType.HoldEndUnjudge:
                    return true;
            }
            return false;
        }

        /// <summary>
        /// ダイナミックノーツ以外の、地上のノーツか (HoldMeshRelay も含む)
        /// </summary>
        public static bool IsGroundNote(DeploymentNoteType type)
        {
            return CanCarry(type) || type == DeploymentNoteType.HoldMeshRelay;
        }

        public static string GetReasonText(DynamicViolationReason reason)
        {
            switch (reason)
            {
                case DynamicViolationReason.NoNoteUnder: return "下にノーツが無い";
                case DynamicViolationReason.WidthMismatch: return "幅が下のノーツと合わない";
                case DynamicViolationReason.OnMeshRelay: return "HoldMeshRelay の上にある";
                case DynamicViolationReason.SeveralNotesUnder: return "同じ幅のノーツが2つ以上重なっている";
                case DynamicViolationReason.NoteAlreadyHasDynamic: return "1つのノーツに2つ以上載っている";
            }
            return string.Empty;
        }
    }

    /// <summary>
    /// ダイナミックノーツが、ルールに合わない理由
    /// </summary>
    public enum DynamicViolationReason
    {
        None,
        NoNoteUnder,
        WidthMismatch,
        OnMeshRelay,
        SeveralNotesUnder,
        NoteAlreadyHasDynamic,
    }

    /// <summary>
    /// どのダイナミックノーツが、どのノーツに載っているか (画面のオブジェクトを使わない部分。Unity の外の確認用のプログラムでも動く)
    /// </summary>
    public class DynamicNoteLinker
    {
        // ダイナミックノーツ → 下のノーツ / 下のノーツ → ダイナミックノーツ (今、両方とも譜面にあるもの)
        readonly Dictionary<IDeployableNoteData, IDeployableNoteData> hostOfDynamic = new Dictionary<IDeployableNoteData, IDeployableNoteData>();
        readonly Dictionary<IDeployableNoteData, IDeployableNoteData> dynamicOfHost = new Dictionary<IDeployableNoteData, IDeployableNoteData>();
        // 前に載っていたノーツ (消して元に戻したときなどに、同じノーツに載り直すため。譜面から外れている間も覚えておく)
        readonly Dictionary<IDeployableNoteData, IDeployableNoteData> rememberedHost = new Dictionary<IDeployableNoteData, IDeployableNoteData>();
        // 2つの場所を最後にそろえたときの場所 (どちらが動いたかを見分ける)
        readonly Dictionary<IDeployableNoteData, AddressKey> synced = new Dictionary<IDeployableNoteData, AddressKey>();
        // ルールに合わないダイナミックノーツと、その理由・調べたときの場所
        readonly Dictionary<IDeployableNoteData, DynamicViolationReason> violations = new Dictionary<IDeployableNoteData, DynamicViolationReason>();
        readonly Dictionary<IDeployableNoteData, AddressKey> violationAddresses = new Dictionary<IDeployableNoteData, AddressKey>();

        readonly HashSet<IDeployableNoteData> live = new HashSet<IDeployableNoteData>();
        readonly Dictionary<long, List<IDeployableNoteData>> groundNotesByLine = new Dictionary<long, List<IDeployableNoteData>>();
        readonly List<IDeployableNoteData> dynamics = new List<IDeployableNoteData>();
        readonly List<IDeployableNoteData> unlinkBuffer = new List<IDeployableNoteData>();

        public int ViolationCount => violations.Count;
        public IEnumerable<IDeployableNoteData> Violations => violations.Keys;
        public int LinkCount => hostOfDynamic.Count;

        public IDeployableNoteData GetDynamicOn(IDeployableNoteData host)
        {
            return host != null && dynamicOfHost.TryGetValue(host, out var dynamic) ? dynamic : null;
        }

        public IDeployableNoteData GetHostOf(IDeployableNoteData dynamic)
        {
            return dynamic != null && hostOfDynamic.TryGetValue(dynamic, out var host) ? host : null;
        }

        public bool IsViolation(IDeployableNoteData note)
        {
            return note != null && violations.ContainsKey(note);
        }

        public DynamicViolationReason GetViolationReason(IDeployableNoteData note)
        {
            return note != null && violations.TryGetValue(note, out var reason) ? reason : DynamicViolationReason.None;
        }

        public void Remember(IDeployableNoteData host, IDeployableNoteData dynamic)
        {
            if (host == null || dynamic == null) { return; }
            rememberedHost[dynamic] = host;
        }

        public void Clear()
        {
            hostOfDynamic.Clear();
            dynamicOfHost.Clear();
            rememberedHost.Clear();
            synced.Clear();
            violations.Clear();
            violationAddresses.Clear();
            live.Clear();
            groundNotesByLine.Clear();
        }

        /// <summary>
        /// 譜面にあるノーツから、どのダイナミックノーツがどのノーツに載っているかを決め直す
        /// </summary>
        public void Reconcile(IReadOnlyList<IDeployableNoteData> liveNotes)
        {
            live.Clear();
            dynamics.Clear();
            foreach (var list in groundNotesByLine.Values) { list.Clear(); }

            for (int i = 0; i < liveNotes.Count; i++)
            {
                var note = liveNotes[i];
                if (note == null || note.Address == null || !live.Add(note)) { continue; }

                var type = note.NoteType;
                if (DynamicNoteRules.IsDynamic(type))
                {
                    dynamics.Add(note);
                }
                else if (DynamicNoteRules.IsGroundNote(type))
                {
                    long key = LineKey(note.Address);
                    if (!groundNotesByLine.TryGetValue(key, out var list))
                    {
                        list = new List<IDeployableNoteData>();
                        groundNotesByLine.Add(key, list);
                    }
                    list.Add(note);
                }
            }

            // 片方が譜面から無くなった・下のノーツが載せられない種類になった・場所が離れた組は、外す
            // (どのノーツに載っていたかは覚えておく。元に戻したときに、載り直せるように)
            // (同じ幅のノーツが重なっていても、外さない: 録音 20261118_005903)
            unlinkBuffer.Clear();
            foreach (var pair in hostOfDynamic)
            {
                if (!live.Contains(pair.Key) || !live.Contains(pair.Value) || !DynamicNoteRules.CanCarry(pair.Value.NoteType) ||
                    !IsSameAddress(pair.Value.Address, pair.Key.Address))
                {
                    unlinkBuffer.Add(pair.Key);
                }
            }
            foreach (var dynamic in unlinkBuffer) { Unlink(dynamic); }

            violations.Clear();
            violationAddresses.Clear();

            // 前に載っていたノーツに載り直す (置いたとき・貼り付けたとき・元に戻したとき)
            foreach (var dynamic in dynamics)
            {
                if (hostOfDynamic.ContainsKey(dynamic)) { continue; }
                if (!rememberedHost.TryGetValue(dynamic, out var host)) { continue; }

                if (live.Contains(host) && DynamicNoteRules.CanCarry(host.NoteType) && !dynamicOfHost.ContainsKey(host) &&
                    IsSameAddress(host.Address, dynamic.Address))
                {
                    Link(dynamic, host);
                }
            }

            // ほかは、同じ分線・同じ幅のノーツを探す
            foreach (var dynamic in dynamics)
            {
                if (hostOfDynamic.ContainsKey(dynamic)) { continue; }

                var reason = TryLinkByAddress(dynamic);
                if (reason != DynamicViolationReason.None)
                {
                    violations[dynamic] = reason;
                    violationAddresses[dynamic] = AddressKey.From(dynamic.Address);
                }
            }
        }

        private DynamicViolationReason TryLinkByAddress(IDeployableNoteData dynamic)
        {
            var address = dynamic.Address;
            if (!groundNotesByLine.TryGetValue(LineKey(address), out var candidates) || candidates.Count == 0)
            {
                return DynamicViolationReason.NoNoteUnder;
            }

            // 同じ分線・同じ幅の、載せられるノーツ。重なっていれば、まだ何も載っていないものを (録音 20261118_005903)
            // (置くときと同じ順に選ぶ: CarrierRank。読み込み直したときに、判定ありの終点に載り替わって、判定が消えないように)
            IDeployableNoteData free = null;
            int exactCount = 0;
            bool isOnMeshRelay = false;
            bool isOverlapping = false;
            foreach (var note in candidates)
            {
                if (IsSameRange(note.Address, address))
                {
                    if (DynamicNoteRules.CanCarry(note.NoteType))
                    {
                        exactCount++;
                        if (!dynamicOfHost.ContainsKey(note) && (free == null || CarrierRank(note) < CarrierRank(free))) { free = note; }
                    }
                    else
                    {
                        isOnMeshRelay = true;
                    }
                }
                else if (IsOverlapping(note.Address, address))
                {
                    isOverlapping = true;
                }
            }

            if (exactCount >= 1)
            {
                if (free == null) { return DynamicViolationReason.NoteAlreadyHasDynamic; }

                Link(dynamic, free);
                return DynamicViolationReason.None;
            }
            if (isOnMeshRelay) { return DynamicViolationReason.OnMeshRelay; }
            if (isOverlapping) { return DynamicViolationReason.WidthMismatch; }
            return DynamicViolationReason.NoNoteUnder;
        }

        /// <summary>
        /// 組が、まだ同じ場所にあるかを調べる (ノーツは動かさない。一緒に動かすのは、動かす操作の側)
        /// </summary>
        /// <returns>決め直しが要るとき true (片方だけが動いた、下のノーツが載せられない種類になった、ルールに合わないダイナミックノーツが動いた)</returns>
        public bool Sync()
        {
            bool needsReconcile = false;

            foreach (var pair in hostOfDynamic)
            {
                var dynamic = pair.Key;
                var host = pair.Value;
                if (!DynamicNoteRules.CanCarry(host.NoteType))
                {
                    needsReconcile = true;
                    continue;
                }

                if (synced.TryGetValue(dynamic, out var key) && key.Matches(host.Address) && key.Matches(dynamic.Address)) { continue; }

                // 両方が同じだけ動いた (両方を動かす操作): 覚えている場所を新しくする
                if (IsSameAddress(host.Address, dynamic.Address))
                {
                    synced[dynamic] = AddressKey.From(host.Address);
                    continue;
                }

                // 片方だけが動いた: 次の決め直しで組を外す
                needsReconcile = true;
            }

            foreach (var pair in violationAddresses)
            {
                if (!pair.Value.Matches(pair.Key.Address)) { needsReconcile = true; }
            }

            return needsReconcile;
        }

        /// <summary>
        /// その場所 (同じ分線・同じ幅) にある、ダイナミックノーツを載せられるノーツの数 (最後に決め直したときの譜面で数える)
        /// </summary>
        public int CountCarriersAt(IReadOnlyAddressWithinRange address)
        {
            if (address == null || !groundNotesByLine.TryGetValue(LineKey(address), out var candidates)) { return 0; }

            int count = 0;
            foreach (var note in candidates)
            {
                if (DynamicNoteRules.CanCarry(note.NoteType) && IsSameRange(note.Address, address)) { count++; }
            }
            return count;
        }

        /// <summary>
        /// その場所 (同じ分線・同じ幅) の、ダイナミックノーツを載せられるノーツのうち、まだ何も載っていないもの (無ければ null)
        /// </summary>
        public IDeployableNoteData FindFreeCarrierAt(IReadOnlyAddressWithinRange address)
        {
            if (address == null || !groundNotesByLine.TryGetValue(LineKey(address), out var candidates)) { return null; }

            IDeployableNoteData best = null;
            foreach (var note in candidates)
            {
                if (!DynamicNoteRules.CanCarry(note.NoteType) || !IsSameRange(note.Address, address) || dynamicOfHost.ContainsKey(note)) { continue; }
                if (best == null || CarrierRank(note) < CarrierRank(best)) { best = note; }
            }
            return best;
        }

        /// <summary>
        /// 重なったノーツのうち、どれに載せるかの順 (小さいほど先): 判定なしの終点、ほかのノーツ、判定ありの終点
        /// (判定ありの終点に載せると、判定なしになるので、ほかに載せられるなら、そちらへ)
        /// </summary>
        private static int CarrierRank(IDeployableNoteData note)
        {
            switch (note.NoteType)
            {
                case DeploymentNoteType.HoldEndUnjudge: return 0;
                case DeploymentNoteType.HoldEnd: return 2;
                default: return 1;
            }
        }

        /// <summary>
        /// 譜面から無くなったノーツの組を外す (どのノーツに載っていたかは覚えておく。元に戻したときに、載り直せるように)
        /// </summary>
        public void ForgetRemoved(IDeployableNoteData note)
        {
            if (note == null) { return; }
            if (hostOfDynamic.ContainsKey(note))
            {
                Unlink(note);
                return;
            }
            if (dynamicOfHost.TryGetValue(note, out var dynamic)) { Unlink(dynamic); }
        }

        private void Link(IDeployableNoteData dynamic, IDeployableNoteData host)
        {
            hostOfDynamic[dynamic] = host;
            dynamicOfHost[host] = dynamic;
            rememberedHost[dynamic] = host;
            synced[dynamic] = AddressKey.From(host.Address);
        }

        private void Unlink(IDeployableNoteData dynamic)
        {
            if (hostOfDynamic.TryGetValue(dynamic, out var host))
            {
                hostOfDynamic.Remove(dynamic);
                if (dynamicOfHost.TryGetValue(host, out var current) && ReferenceEquals(current, dynamic)) { dynamicOfHost.Remove(host); }
            }
            synced.Remove(dynamic);
        }

        private static long LineKey(IReadOnlyAddressWithinRange address)
        {
            return ((long)address.BarIndex << 32) | (uint)address.SubDivisionIndex;
        }

        public static bool IsSameAddress(IReadOnlyAddressWithinRange a, IReadOnlyAddressWithinRange b)
        {
            if (a == null || b == null) { return false; }
            return a.BarIndex == b.BarIndex && a.SubDivisionIndex == b.SubDivisionIndex && IsSameRange(a, b);
        }

        private static bool IsSameRange(IReadOnlyAddressWithinRange a, IReadOnlyAddressWithinRange b)
        {
            var rangeA = a.RangeRP;
            var rangeB = b.RangeRP;
            if (rangeA.Count != rangeB.Count) { return false; }
            for (int i = 0; i < rangeA.Count; i++)
            {
                if (rangeA[i] != rangeB[i]) { return false; }
            }
            return true;
        }

        private static bool IsOverlapping(IReadOnlyAddressWithinRange a, IReadOnlyAddressWithinRange b)
        {
            var rangeA = a.RangeRP;
            var rangeB = b.RangeRP;
            for (int i = 0; i < rangeA.Count; i++)
            {
                for (int j = 0; j < rangeB.Count; j++)
                {
                    if (rangeA[i] == rangeB[j]) { return true; }
                }
            }
            return false;
        }

        /// <summary>
        /// ある時点の場所 (小節・分線・レーン) の写し
        /// </summary>
        private readonly struct AddressKey
        {
            readonly int bar;
            readonly int sub;
            readonly float[] range;

            AddressKey(int bar, int sub, float[] range)
            {
                this.bar = bar;
                this.sub = sub;
                this.range = range;
            }

            public static AddressKey From(IReadOnlyAddressWithinRange address)
            {
                var source = address.RangeRP;
                var range = new float[source.Count];
                for (int i = 0; i < range.Length; i++) { range[i] = source[i]; }
                return new AddressKey(address.BarIndex, address.SubDivisionIndex, range);
            }

            public bool Matches(IReadOnlyAddressWithinRange address)
            {
                if (address == null || range == null) { return false; }
                if (address.BarIndex != bar || address.SubDivisionIndex != sub) { return false; }

                var source = address.RangeRP;
                if (source.Count != range.Length) { return false; }
                for (int i = 0; i < range.Length; i++)
                {
                    if (source[i] != range[i]) { return false; }
                }
                return true;
            }
        }
    }
}

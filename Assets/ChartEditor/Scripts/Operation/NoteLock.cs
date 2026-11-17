using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer.Unity;

namespace ChartEditor
{
    /// <summary>
    /// ノーツのロック (右クリックのメニューの「ロックする」)
    ///
    /// ロックしたノーツは、選べるが、動かせない・消せない・種類を変えられない (うっかり触らないように)。
    /// ・ほかのノーツと同じように選べる (クリック・Ctrl+クリック・範囲選択。選んだ枠も出る。録音 20261117_005326)
    /// ・カーソルの下にあるとき、選ぶことだけできる (CursorInteracter: 移動・幅のモードにせず、道具には選ぶための部品だけを渡す)
    /// ・選択に対する操作 (移動・幅・削除・反転・接続・順番・種類・形の編集) は、ロックしたノーツを飛ばす (コピーはできる)
    /// ・右クリックのメニューから「ロックを外す」を選べる。すべて外すのは、譜面メニューから
    /// ・淡い色で表示し (シェーダーの _Pale)、帯より手前に描き (sortingOrder)、右上に鍵の印を出す (NoteLockIconsView)
    ///   (次のノーツまでの帯は、ノーツ自身の絵ではないので、元の色のまま。録音 20261117_012356)
    /// ロックは編集している間だけのもので、譜面ファイルには保存しない。別の譜面を開くと、すべて外れる。
    /// </summary>
    public class NoteLock : ITickable, IDisposable
    {
        static NoteLock instance;

        // 白黒の表示を付け直す間隔 (秒。ノーツの絵は作り直されることがあるので、ときどき付け直す)
        const float REFRESH_INTERVAL_SECONDS = 0.25f;
        // (最初に使うときに求める。型を読み込んだ時点では求めない: Unity の外で動かす確認用のプログラムでも、この型を使えるように)
        static int paleIdCache = -1;
        static int paleId => paleIdCache >= 0 ? paleIdCache : (paleIdCache = Shader.PropertyToID("_Pale"));
        // ロックしたメッシュ中継点 (帯と同じ青で薄い) を、白くはっきり出す (録音 20261117_162135)
        static int paleStrongIdCache = -1;
        static int paleStrongId => paleStrongIdCache >= 0 ? paleStrongIdCache : (paleStrongIdCache = Shader.PropertyToID("_PaleStrong"));

        readonly IChartEditorDataGetter dataGetter;
        readonly INotesDataGetter notesGetter;
        readonly INotesDataSetter notesSetter;

        readonly HashSet<IDeployableNoteData> locked = new HashSet<IDeployableNoteData>();
        // ロックしたノーツの相手 (ダイナミックノーツと下のノーツ)。相手がロックしているので、ロックして見えるもの
        // (相手から外れたら、ロックも外れる。ロックの付け直しのたびに作り直す)
        readonly HashSet<IDeployableNoteData> partnerLocked = new HashSet<IDeployableNoteData>();
        readonly HashSet<Renderer> grayRenderers = new HashSet<Renderer>();
        readonly List<Renderer> rendererBuffer = new List<Renderer>();
        readonly HashSet<IDeployableNoteData> liveNotes = new HashSet<IDeployableNoteData>();
        readonly List<IDeployableNoteData> partnerBuffer = new List<IDeployableNoteData>();
        // 手前に描くようにした絵と、元の描く順番
        readonly Dictionary<Renderer, int> raisedRenderers = new Dictionary<Renderer, int>();
        // ロックしたノーツを、帯 (ホールドのメッシュ) より手前に描くために、描く順番に足す数
        const int LOCKED_SORTING_ORDER_OFFSET = 1;
        static readonly List<INoteConnectionMesh> connectionBuffer = new List<INoteConnectionMesh>();
        MaterialPropertyBlock propertyBlock;
        ChartData lockedChart;
        float nextRefreshTime;

        public NoteLock(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter, INotesDataSetter notesSetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;
            instance = this;
        }

        void IDisposable.Dispose()
        {
            if (instance == this) { instance = null; }
        }

        /// <summary>ロックしているノーツが、1つでもあるか</summary>
        public static bool HasAny => instance != null && instance.locked.Count > 0;

        /// <summary>そのノーツをロックしているか (ダイナミックノーツと下のノーツは、どちらかをロックしていれば、両方ロック)</summary>
        public static bool IsLocked(IDeployableNoteData note)
        {
            if (instance == null || note == null) { return false; }
            if (instance.locked.Contains(note)) { return true; }

            var partner = DynamicNoteAttachments.GetPartner(note);
            return partner != null && instance.locked.Contains(partner);
        }

        /// <summary>ロックしているノーツの数</summary>
        public static int Count => instance != null ? instance.locked.Count : 0;

        /// <summary>
        /// ノーツをロックする (選択はそのまま。頂点レイヤーで編集する対象だったら、外す)
        /// </summary>
        public static void Lock(IEnumerable<IDeployableNoteData> notes)
        {
            if (instance == null || notes == null) { return; }

            instance.BindChart();
            // ダイナミックノーツと下のノーツは、どちらをロックしても両方ロックして見える (相手のロックは partnerLocked で、組が外れたら外れる)
            // ルールに合わないダイナミックノーツは、ロックしない
            var list = new List<IDeployableNoteData>(notes);
            int looseCount = list.RemoveAll(DynamicNoteAttachments.IsViolation);
            DynamicNoteAttachments.LogRefused(looseCount, "ルールに合わないダイナミックノーツは、ロックできません");
            foreach (var note in list)
            {
                if (note == null) { continue; }
                instance.locked.Add(note);
                if (ReferenceEquals(instance.notesGetter.EditingVertices.Value, note)) { instance.notesSetter.SetEditingVertices(null); }
            }
            instance.nextRefreshTime = 0f;
        }

        /// <summary>
        /// ノーツのロックを外す
        /// </summary>
        public static void Unlock(IEnumerable<IDeployableNoteData> notes)
        {
            if (instance == null || notes == null) { return; }

            // ダイナミックノーツと下のノーツは、どちらのロックを外しても両方外す
            foreach (var note in DynamicNoteAttachments.WithPartners(notes))
            {
                instance.locked.Remove(note);
                instance.partnerLocked.Remove(note);
            }
            instance.RebuildPartnerLocked();
            instance.RestoreColors();
            instance.nextRefreshTime = 0f;
        }

        /// <summary>
        /// ロックしたノーツへの操作を受け付けなかったことを、ログに出す (action は「消し」「動かし」など。「〜ませんでした」が付く)
        /// </summary>
        public static void LogRefused(int count, string action)
        {
            if (count <= 0) { return; }
            Debug.Log($"【ロック】ロックしたノーツ {count} 個は、{action}ませんでした (外すには、右クリックのメニューの「ロックを外す」)");
        }

        /// <summary>
        /// ノーツ自身の絵を集める (次のノーツまでの帯は除く)
        /// </summary>
        public static void CollectBodyRenderers(NoteObject noteObject, List<Renderer> results)
        {
            results.Clear();
            if (noteObject == null) { return; }

            noteObject.GetComponentsInChildren(true, results);
            noteObject.GetComponentsInChildren(true, connectionBuffer);
            foreach (var owner in connectionBuffer)
            {
                GameObject mesh = owner?.ConnectionMesh;
                if (mesh == null) { continue; }
                Transform meshTransform = mesh.transform;
                // (鍵の印の場所を毎フレーム求めるので、ラムダ式を使わずに外す)
                for (int i = results.Count - 1; i >= 0; i--)
                {
                    if (results[i] == null || results[i].transform.IsChildOf(meshTransform)) { results.RemoveAt(i); }
                }
            }
            connectionBuffer.Clear();
        }

        /// <summary>
        /// 画面に出ている、ロックしたノーツの絵を集める (鍵の印を出すため)
        /// </summary>
        public static void CollectLockedObjects(List<NoteObject> results)
        {
            results.Clear();
            if (instance == null || instance.locked.Count == 0) { return; }

            var allNotes = instance.notesGetter.DataToNoteObject;
            for (int i = 0; i < allNotes.Count; i++)
            {
                var pair = allNotes[i];
                if (pair.Object == null || !instance.IsShownLocked(pair.Data)) { continue; }
                if (!pair.Object.gameObject.activeInHierarchy) { continue; }
                results.Add(pair.Object);
            }
        }

        /// <summary>
        /// 鍵の印を出してよい画面か (グラウンド・スペースのレイヤー)
        /// </summary>
        public static bool IsIconLayer => instance != null && !instance.dataGetter.IsLoadingChart.Value &&
            (instance.dataGetter.EditNoteType.Value == EditNoteType.Ground || instance.dataGetter.EditNoteType.Value == EditNoteType.Space) &&
            // 全画面のプレビュー・3D の編集の間は、レーンの画面が見えていないので出さない
            instance.dataGetter.CurrentEditMode.Value != EditMode.Preview &&
            instance.dataGetter.CurrentEditMode.Value != EditMode.SpaceHoldView;

        /// <summary>
        /// すべてのロックを外す (譜面メニューの「すべてのロックを外す」)
        /// </summary>
        public static void UnlockAll()
        {
            if (instance == null) { return; }

            instance.locked.Clear();
            instance.partnerLocked.Clear();
            instance.RestoreColors();
        }

        void ITickable.Tick()
        {
            if (locked.Count == 0 && grayRenderers.Count == 0) { return; }

            // 別の譜面を開いたら、すべて外す
            if (!ReferenceEquals(dataGetter.ChartData.Value, lockedChart) || dataGetter.IsLoadingChart.Value)
            {
                locked.Clear();
                partnerLocked.Clear();
                RestoreColors();
                lockedChart = dataGetter.ChartData.Value;
                return;
            }

            if (Time.unscaledTime < nextRefreshTime) { return; }
            nextRefreshTime = Time.unscaledTime + REFRESH_INTERVAL_SECONDS;

            // 譜面から無くなったノーツ (元に戻す・やり直しで消えたものなど) は、外す
            // (ノーツの一覧を1回だけ見て決める。ロックしたノーツごとに一覧を探すと、たくさんロックしたときに重くなる)
            var allNotes = notesGetter.DataToNoteObject;
            liveNotes.Clear();
            for (int i = 0; i < allNotes.Count; i++)
            {
                if (allNotes[i].Object != null) { liveNotes.Add(allNotes[i].Data); }
            }
            locked.RemoveWhere(note => !liveNotes.Contains(note));
            liveNotes.Clear();

            // ダイナミックノーツと下のノーツは、片方だけロックしていても、両方を淡く表示する
            // (元に戻す・やり直しで、ロックしていない方が載り直したときなど。組が外れたら、相手は元の色に戻す)
            if (RebuildPartnerLocked()) { RestoreColors(); }

            // ロックしたノーツの絵を淡くする (絵が作り直されても、付け直す)
            grayRenderers.RemoveWhere(renderer => renderer == null);
            for (int i = 0; i < allNotes.Count; i++)
            {
                var pair = allNotes[i];
                if (pair.Object == null || !IsShownLocked(pair.Data)) { continue; }

                CollectBodyRenderers(pair.Object, rendererBuffer);
                float strong = IsMeshRelay(pair.Data) ? 1f : 0f;
                foreach (var renderer in rendererBuffer)
                {
                    grayRenderers.Add(renderer);
                    SetMonochrome(renderer, 1f, strong);
                    // 帯より手前に描く (淡い中継点が、帯の下に埋もれないように)
                    if (!raisedRenderers.ContainsKey(renderer))
                    {
                        raisedRenderers.Add(renderer, renderer.sortingOrder);
                        renderer.sortingOrder += LOCKED_SORTING_ORDER_OFFSET;
                    }
                }
            }
            rendererBuffer.Clear();
        }

        // ロックして見えるか (自分でロックした、または相手がロックした)
        private bool IsShownLocked(IDeployableNoteData note) => locked.Contains(note) || partnerLocked.Contains(note);

        /// <summary>
        /// ロックしたノーツの相手を、作り直す
        /// </summary>
        /// <returns>前は相手としてロックして見えていたノーツが、外れたとき true (元の色に戻すため)</returns>
        private bool RebuildPartnerLocked()
        {
            partnerBuffer.Clear();
            foreach (var note in locked)
            {
                var partner = DynamicNoteAttachments.GetPartner(note);
                if (partner != null && !locked.Contains(partner)) { partnerBuffer.Add(partner); }
            }

            bool isLost = false;
            foreach (var old in partnerLocked)
            {
                if (!partnerBuffer.Contains(old)) { isLost = true; break; }
            }
            partnerLocked.Clear();
            foreach (var partner in partnerBuffer) { partnerLocked.Add(partner); }
            partnerBuffer.Clear();
            return isLost;
        }

        private void BindChart()
        {
            if (!ReferenceEquals(lockedChart, dataGetter.ChartData.Value))
            {
                locked.Clear();
                partnerLocked.Clear();
                RestoreColors();
                lockedChart = dataGetter.ChartData.Value;
            }
        }

        /// <summary>
        /// 淡くした絵を、元の色に戻す (ロックしたままのノーツは、次の付け直しで、また淡くなる)
        /// </summary>
        private void RestoreColors()
        {
            var allNotes = notesGetter.DataToNoteObject;
            var stillGray = new HashSet<Renderer>();
            for (int i = 0; i < allNotes.Count; i++)
            {
                var pair = allNotes[i];
                if (pair.Object == null) { continue; }
                if (!IsShownLocked(pair.Data)) { continue; }

                CollectBodyRenderers(pair.Object, rendererBuffer);
                foreach (var renderer in rendererBuffer) { stillGray.Add(renderer); }
            }
            rendererBuffer.Clear();

            foreach (var renderer in grayRenderers)
            {
                if (renderer != null && !stillGray.Contains(renderer)) { SetMonochrome(renderer, 0f, 0f); }
            }
            grayRenderers.Clear();

            // 描く順番を戻す (ロックしたままのものは、次の付け直しで、また手前になる)
            foreach (var pair in raisedRenderers)
            {
                if (pair.Key != null && !stillGray.Contains(pair.Key)) { pair.Key.sortingOrder = pair.Value; }
            }
            var keep = new List<KeyValuePair<Renderer, int>>();
            foreach (var pair in raisedRenderers)
            {
                if (pair.Key != null && stillGray.Contains(pair.Key)) { keep.Add(pair); }
            }
            raisedRenderers.Clear();
            foreach (var pair in keep) { raisedRenderers.Add(pair.Key, pair.Value); }
            nextRefreshTime = 0f;
        }

        private void SetMonochrome(Renderer renderer, float amount, float strong)
        {
            propertyBlock ??= new MaterialPropertyBlock();

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(paleId, amount);
            propertyBlock.SetFloat(paleStrongId, strong);
            renderer.SetPropertyBlock(propertyBlock);
        }

        /// <summary>
        /// メッシュ中継点か (いつもは帯と同じ青で薄く描くので、ロックしたときは、白くはっきり出す)
        /// </summary>
        private static bool IsMeshRelay(IDeployableNoteData note)
        {
            if (note is not ITypeChangableNoteData typed) { return false; }
            var type = typed.NoteTypeRP.Value;
            return type == DeploymentNoteType.HoldMeshRelay || type == DeploymentNoteType.SpaceHoldMeshRelay;
        }
    }
}

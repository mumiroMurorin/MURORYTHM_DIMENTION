using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer.Unity;

namespace ChartEditor
{
    /// <summary>
    /// ホールドを、1本だけ編集する
    ///
    /// グラウンドのレイヤーで、ホールドのノーツ (始点・中継点・メッシュ中継点・終点のどれでも) をダブルクリックすると、
    /// そのホールド以外のノーツが白黒になり、触れなくなる。重なり合ったホールドのうち、1本だけを動かしたいときに使う。
    ///
    /// ・触れなくなるのは、始めた時点で譜面にあった、ほかのノーツ
    /// ・この間は、ノーツを新しく置くことも、貼り付けることもできない。置けるノーツの一覧も隠す
    ///   (できるのは、そのホールドのノーツを動かす・幅を変える・曲げる・消す、など)
    /// ・ほかのノーツは、消えるわけではない。カーソルは、ほかのノーツを通り抜けて、その奥にあるものを指す。
    ///   範囲選択や、プレビューからの選択でも、ほかのノーツは選ばれない。
    ///   ノーツの重なりの検査 (レギュレーション違反の赤い縁取り) は、これまでどおり、すべてのノーツで行われる
    /// ・戻り方: Esc キー、何も無い所 (または白黒のノーツ) をダブルクリック、レイヤーの切り替え、譜面の読み込み
    /// ・白黒になっている別のホールドをダブルクリックすると、そちらのホールドに切り替わる
    ///
    /// 白黒にするのは、ノーツのシェーダー (Custom/UnlitFade) の _Monochrome を、ノーツの絵ごとに 1 にすることで行う。
    /// 画面の上の「ホールドを1本だけ編集中」の表示は、シーンに置いてある HoldIsolationBannerView。
    /// </summary>
    public class HoldIsolation : ITickable, IDisposable
    {
        // 白黒の表示を、付け直す間隔 (秒)。ホールドの帯などは、形が変わるたびに作り直されるので、ときどき付け直す
        const float REFRESH_INTERVAL_SECONDS = 0.5f;
        // カーソルの下を調べる距離と、一度に調べる当たり判定の数
        const float RAY_DISTANCE = 100f;
        const int MAX_HIT_COUNT = 64;
        const string RAYCAST_LAYER_NAME = "CursorInteractable";

        // 受け付けないモード (ほかの操作の途中、ダイアログや説明書を開いている間、全画面プレビュー)
        static readonly EditMode[] ignoreEditModes =
        {
            EditMode.Connecting,
            EditMode.Moving,
            EditMode.Scaling,
            EditMode.SpaceMoving,
            EditMode.CurveHandling,
            EditMode.EditingBarConfig,
            EditMode.EditingSubDivisionConfig,
            EditMode.Explanation,
            EditMode.Preview,
        };

        // (最初に使うときに求める。型を読み込んだ時点では求めない: Unity の外で動かす確認用のプログラムでも、この型を使えるように)
        static int monochromeIdCache = -1;
        static int monochromeId => monochromeIdCache >= 0 ? monochromeIdCache : (monochromeIdCache = Shader.PropertyToID("_Monochrome"));

        static HoldIsolation instance;
        static readonly RaycastHit[] hitBuffer = new RaycastHit[MAX_HIT_COUNT];

        readonly IChartEditorDataGetter dataGetter;
        readonly INotesDataGetter notesGetter;
        readonly INotesDataSetter notesSetter;

        HoldIsolationBannerView banner;
        // 置けるノーツの一覧 (編集している間は隠す)
        NotesViewportView notesView;
        bool hasSearchedScene;

        bool isActive;
        // 触れなくするノーツ (始めた時点で譜面にあった、ほかのノーツ)
        readonly HashSet<IDeployableNoteData> blocked = new HashSet<IDeployableNoteData>();
        // 編集しているホールドのノーツ (始めた時点のもの。すべて譜面から無くなったら、戻る)
        readonly List<IDeployableNoteData> members = new List<IDeployableNoteData>();
        // 始めたときの譜面 (譜面が入れ替わったら、戻る)
        ChartData isolatedChart;

        // 白黒にしてあるノーツの絵
        readonly HashSet<Renderer> grayRenderers = new HashSet<Renderer>();
        readonly List<Renderer> rendererBuffer = new List<Renderer>();
        readonly List<IDeployableNoteData> noteBuffer = new List<IDeployableNoteData>();
        MaterialPropertyBlock propertyBlock;
        float nextRefreshTime;

        /// <summary>
        /// クリックした場所にあったもの
        /// </summary>
        private enum ClickTarget
        {
            // ノーツが無い (レーンだけ、または触れないノーツのうちホールドでないものだけ)
            Nothing,
            // 触れるホールド
            Hold,
            // 触れなくしてあるホールド
            BlockedHold,
            // 触れる、ホールド以外のノーツ
            OtherNote,
        }

        // 前回のクリック (ダブルクリックを見分ける。2回のクリックの間隔は 0.35 秒まで、カーソルのずれは 12 ピクセルまで)
        readonly DoubleClickDetector doubleClick = new DoubleClickDetector(DoubleClickDetector.MAX_INTERVAL, DoubleClickDetector.POINTER_MAX_DISTANCE);
        ClickTarget lastClickTarget;
        NoteData_Hold lastClickHold;

        /// <summary>
        /// ホールドを、1本だけ編集している最中か
        /// </summary>
        public static bool IsActive => instance != null && instance.isActive;

        /// <summary>
        /// そのノーツは、今、触れなくしてあるか (ホールドを1本だけ編集している間のほかのノーツ)
        /// (ロックしたノーツは、選べるので含めない。録音 20261117_005326)
        /// </summary>
        public static bool IsBlocked(IDeployableNoteData note)
        {
            return IsBlockedByIsolation(note);
        }

        /// <summary>
        /// そのノーツは、ホールドを1本だけ編集しているために、触れなくしてあるか (ロックは含めない)
        /// </summary>
        public static bool IsBlockedByIsolation(IDeployableNoteData note)
        {
            return instance != null && instance.isActive && note != null && instance.blocked.Contains(note);
        }

        /// <summary>
        /// そのホールドだけを編集する (右クリックのメニューから。ダブルクリックと同じ)
        /// </summary>
        public static void TryEnter(NoteData_Hold hold)
        {
            if (instance == null || hold == null) { return; }
            if (instance.dataGetter.EditNoteType.Value != EditNoteType.Ground) { return; }

            instance.Enter(hold);
        }

        /// <summary>
        /// 光線が当たるもののうち、いちばん手前にある「触れるもの」を返す (1本だけ編集しているホールドのほかのノーツは、通り抜ける)
        /// </summary>
        public static bool TryRaycast(Ray ray, float distance, int layerMask, out RaycastHit hit)
        {
            hit = default;

            int count = Physics.RaycastNonAlloc(ray, hitBuffer, distance, layerMask);
            float nearest = float.MaxValue;
            bool isFound = false;
            for (int i = 0; i < count; i++)
            {
                if (hitBuffer[i].distance >= nearest) { continue; }
                // (通り抜けるのは、1本だけ編集しているホールドのほかのノーツだけ。ロックしたノーツは通り抜けない)
                if (IsBlockedByIsolation(GetNote(hitBuffer[i].collider))) { continue; }

                nearest = hitBuffer[i].distance;
                hit = hitBuffer[i];
                isFound = true;
            }
            return isFound;
        }

        /// <summary>
        /// 当たり判定が付いているノーツ (ノーツの当たり判定でなければ null)
        /// </summary>
        private static IDeployableNoteData GetNote(Collider collider)
        {
            if (collider == null) { return null; }

            var noteObject = collider.GetComponentInParent<NoteObject>();
            return noteObject != null ? noteObject.NoteData : null;
        }

        public HoldIsolation(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter, INotesDataSetter notesSetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
            this.notesSetter = notesSetter;

            instance = this;
        }

        void IDisposable.Dispose()
        {
            if (isActive) { Exit(); }
            if (instance == this) { instance = null; }
        }

        void ITickable.Tick()
        {
            SearchSceneOnce();

            try
            {
                if (isActive)
                {
                    // 譜面を読み込んだ、譜面が入れ替わった、レイヤーを切り替えた: 戻る
                    if (dataGetter.IsLoadingChart.Value ||
                        !ReferenceEquals(dataGetter.ChartData.Value, isolatedChart) ||
                        dataGetter.EditNoteType.Value != EditNoteType.Ground)
                    {
                        Exit();
                        return;
                    }

                    // Esc で戻る (文字の入力中、メニューを開いている間、ほかの操作の途中の Esc は、そちらで使う)
                    // (ホールドの曲線の、位置を数値で決める窓を閉じる Esc は、そちらで使う)
                    if (Input.GetKeyDown(KeyCode.Escape) && !InputFieldFocus.IsTyping && !EditorMenuBar.IsBlockingChartInput &&
                        !HoldCurveEditor.IsUsingEscape &&
                        !dataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes))
                    {
                        Exit();
                        return;
                    }
                }

                HandleClick();

                if (isActive && Time.unscaledTime >= nextRefreshTime) { Refresh(); }
            }
            catch (Exception exception)
            {
                // 途中で失敗しても、ノーツに触れない状態のまま残らないようにする
                Debug.LogException(exception);
                if (isActive) { Exit(); }
            }
        }

        private void SearchSceneOnce()
        {
            if (hasSearchedScene) { return; }
            hasSearchedScene = true;

            banner = UnityEngine.Object.FindObjectOfType<HoldIsolationBannerView>(true);
            if (banner != null) { banner.SetVisible(false); }

            notesView = UnityEngine.Object.FindObjectOfType<NotesViewportView>(true);
        }

        #region ダブルクリック

        /// <summary>
        /// クリックを調べて、ダブルクリックなら、始める・切り替える・戻る
        /// (2回とも、同じホールドの上、または、どちらも何も無い所であること)
        /// </summary>
        private void HandleClick()
        {
            if (!Input.GetMouseButtonDown(0)) { return; }

            // 受け付けない状態のクリックは、数えない
            bool isAcceptable =
                !dataGetter.IsLoadingChart.Value &&
                dataGetter.EditNoteType.Value == EditNoteType.Ground &&
                dataGetter.ChartData.Value != null &&
                !dataGetter.CurrentEditMode.Value.IsInEditModeList(ignoreEditModes) &&
                // 編集ツールのときだけ (接続・タイプ変更のツールでは、同じノーツを続けて2回押すのは、ふつうの操作)
                RangeSelectGesture.IsEditToolIdle(EditNoteType.Ground, dataGetter.CurrentEditMode.Value) &&
                !EditorMenuBar.IsBlockingChartInput &&
                !HoldCurveEditor.IsCapturingPointer &&
                !ModifierKey.Ctrl && !ModifierKey.Shift && !ModifierKey.Alt &&
                (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject());
            if (!isAcceptable)
            {
                doubleClick.Reset();
                return;
            }

            Vector2 position = Input.mousePosition;
            ClickTarget clickTarget = FindClickTarget(position, out NoteData_Hold hold);

            bool isSameTarget =
                clickTarget == lastClickTarget &&
                (hold == null || lastClickHold == null || IsSameChain(hold, lastClickHold));
            bool isDoubleClick = doubleClick.Click(position, isSameTarget);

            lastClickTarget = clickTarget;
            lastClickHold = hold;

            if (!isDoubleClick) { return; }

            switch (clickTarget)
            {
                // 触れるホールド: 始める (もう編集しているホールドなら、そのまま)
                case ClickTarget.Hold:
                    if (!isActive) { Enter(hold); }
                    break;
                // 白黒のホールド: そちらに切り替える
                case ClickTarget.BlockedHold:
                    Enter(hold);
                    break;
                // 何も無い所: 戻る
                case ClickTarget.Nothing:
                    if (isActive) { Exit(); }
                    break;
            }
        }

        /// <summary>
        /// 画面上の位置にあるものを調べる。触れるノーツがあれば、それを優先する
        /// </summary>
        private ClickTarget FindClickTarget(Vector2 screenPosition, out NoteData_Hold hold)
        {
            hold = null;

            Camera camera = EditorViewCameras.GroundView;
            if (camera == null) { return ClickTarget.Nothing; }

            int layerMask = 1 << LayerMask.NameToLayer(RAYCAST_LAYER_NAME);
            int count = Physics.RaycastNonAlloc(camera.ScreenPointToRay(screenPosition), hitBuffer, RAY_DISTANCE, layerMask);

            // いちばん手前の、触れるノーツと、触れなくしてあるホールド
            IDeployableNoteData nearestNote = null;
            float nearestNoteDistance = float.MaxValue;
            NoteData_Hold nearestBlockedHold = null;
            float nearestBlockedDistance = float.MaxValue;

            for (int i = 0; i < count; i++)
            {
                IDeployableNoteData note = GetNote(hitBuffer[i].collider);
                if (note == null) { continue; }

                if (IsBlocked(note))
                {
                    if (note is NoteData_Hold blockedHold && hitBuffer[i].distance < nearestBlockedDistance)
                    {
                        nearestBlockedHold = blockedHold;
                        nearestBlockedDistance = hitBuffer[i].distance;
                    }
                }
                else if (hitBuffer[i].distance < nearestNoteDistance)
                {
                    nearestNote = note;
                    nearestNoteDistance = hitBuffer[i].distance;
                }
            }

            if (nearestNote != null)
            {
                hold = nearestNote as NoteData_Hold;
                return hold != null ? ClickTarget.Hold : ClickTarget.OtherNote;
            }

            if (nearestBlockedHold != null)
            {
                hold = nearestBlockedHold;
                return ClickTarget.BlockedHold;
            }

            return ClickTarget.Nothing;
        }

        /// <summary>
        /// 2つのノーツが、同じホールド (つながり) のものか
        /// </summary>
        private static bool IsSameChain(NoteData_Hold a, NoteData_Hold b)
        {
            if (ReferenceEquals(a, b)) { return true; }
            return a.ChainIndex.Value >= 0 && a.ChainIndex.Value == b.ChainIndex.Value;
        }

        #endregion

        #region 始める・戻る

        /// <summary>
        /// そのノーツがあるホールドだけを、編集できるようにする
        /// </summary>
        private void Enter(NoteData_Hold hold)
        {
            if (hold == null) { return; }

            // 別のホールドを編集していたときは、白黒をいったん元に戻す
            if (isActive) { ClearGray(); }

            // 編集するホールドのノーツ (つながっていないノーツなら、そのノーツだけ)
            members.Clear();
            var chain = hold.ChainIndex.Value >= 0 ? notesGetter.GetChainNoteList(hold.ChainIndex.Value)?.ChainNoteList : null;
            if (chain != null)
            {
                for (int i = 0; i < chain.Count; i++) { members.Add(chain[i]); }
            }
            if (!members.Contains(hold)) { members.Add(hold); }

            // それ以外の、今あるノーツを、触れなくする
            blocked.Clear();
            var allNotes = notesGetter.DataToNoteObject;
            for (int i = 0; i < allNotes.Count; i++)
            {
                IDeployableNoteData note = allNotes[i].Data;
                if (note != null && !members.Contains(note)) { blocked.Add(note); }
            }

            isolatedChart = dataGetter.ChartData.Value;
            isActive = true;

            // 触れなくなったノーツが選ばれたままだと、Delete キーなどで消せてしまうので、選択から外す
            noteBuffer.Clear();
            foreach (var selected in notesGetter.SelectingNotes)
            {
                if (blocked.Contains(selected)) { noteBuffer.Add(selected); }
            }
            foreach (var note in noteBuffer) { notesSetter.TryRemoveSelectingNotes(note); }
            noteBuffer.Clear();

            Refresh();
            if (banner != null) { banner.SetVisible(true); }
            if (notesView != null) { notesView.SetHiddenByIsolation(true); }
            Debug.Log("【ホールドの単独編集】このホールド以外のノーツは、触れなくなりました。Esc キーか、何も無い所のダブルクリックで戻ります");
        }

        /// <summary>
        /// すべてのノーツを、また触れるようにする
        /// </summary>
        private void Exit()
        {
            isActive = false;
            blocked.Clear();
            members.Clear();
            isolatedChart = null;
            doubleClick.Reset();

            ClearGray();
            if (banner != null) { banner.SetVisible(false); }
            if (notesView != null) { notesView.SetHiddenByIsolation(false); }
        }

        /// <summary>
        /// 触れなくしてあるノーツの絵を白黒にする (新しく作られた絵にも付ける)。編集しているホールドが無くなっていたら、戻る
        /// </summary>
        private void Refresh()
        {
            nextRefreshTime = Time.unscaledTime + REFRESH_INTERVAL_SECONDS;

            // 編集しているホールドのノーツが、1つも譜面に残っていなければ、戻る
            bool hasMember = false;
            foreach (var member in members)
            {
                if (notesGetter.GetNoteObject(member) != null)
                {
                    hasMember = true;
                    break;
                }
            }
            if (!hasMember)
            {
                Exit();
                return;
            }

            // 無くなった絵を、覚えから外す
            grayRenderers.RemoveWhere(renderer => renderer == null);

            var allNotes = notesGetter.DataToNoteObject;
            for (int i = 0; i < allNotes.Count; i++)
            {
                var pair = allNotes[i];
                if (pair.Object == null || !blocked.Contains(pair.Data)) { continue; }

                pair.Object.GetComponentsInChildren(true, rendererBuffer);
                foreach (var renderer in rendererBuffer)
                {
                    if (grayRenderers.Add(renderer)) { SetMonochrome(renderer, 1f); }
                }
            }
            rendererBuffer.Clear();
        }

        /// <summary>
        /// 白黒にしてあった絵を、元の色に戻す
        /// </summary>
        private void ClearGray()
        {
            foreach (var renderer in grayRenderers)
            {
                if (renderer != null) { SetMonochrome(renderer, 0f); }
            }
            grayRenderers.Clear();
        }

        /// <summary>
        /// 絵の、白黒にする度合いを変える (0 でそのままの色、1 で白黒)
        /// マテリアルは複製しない。絵ごとの上書きとして付ける (マテリアルを差し替えられても、上書きは残る)
        /// </summary>
        private void SetMonochrome(Renderer renderer, float amount)
        {
            propertyBlock ??= new MaterialPropertyBlock();

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(monochromeId, amount);
            renderer.SetPropertyBlock(propertyBlock);
        }

        #endregion
    }
}

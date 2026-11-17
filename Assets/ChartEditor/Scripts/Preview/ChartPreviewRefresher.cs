using ChartConvert;
using Cysharp.Threading.Tasks;
using System;
using System.Diagnostics;
using UniRx;
using UnityEngine;
using VContainer;
using Debug = UnityEngine.Debug;

namespace ChartEditor
{
    /// <summary>
    /// 編集中の譜面をプレビューに反映する
    ///
    /// ・譜面を編集すると、少し間を置いて自動で反映する
    /// ・反映は「本編用のデータへ変換 → 今のプレビューとの差分を生成」の順で行う
    ///   変換は一気に行い、ノーツの生成は ChartPreviewGenerator が少しずつ進める
    /// </summary>
    public class ChartPreviewRefresher : MonoBehaviour, IChartPreviewRefreshable
    {
        [SerializeField] ChartPreviewGenerator previewGenerator;
        [SerializeField] GroundControllerPreview groundController;

        [Header("自動更新")]
        [Tooltip("譜面を編集したら、自動でプレビューに反映する")]
        [SerializeField] bool isAutoRefreshEnabled = true;
        [Tooltip("最後の編集からこの秒数たったら反映する (続けて行った編集を1回にまとめるため)")]
        [SerializeField] float autoRefreshDelaySeconds = 0.25f;

        [Header("表示とログ")]
        [Tooltip("ノーツの生成がこの秒数より長引いたら、ログ欄に進み具合を表示する")]
        [SerializeField] float progressNoticeDelaySeconds = 0.5f;
        [Tooltip("自動更新の変換にこのミリ秒以上かかったら、所要時間をログに出す")]
        [SerializeField] float slowRefreshLogMilliseconds = 30f;
        [Tooltip("自動更新の所要時間ログを出す最短の間隔(秒)")]
        [SerializeField] float slowRefreshLogIntervalSeconds = 10f;
        [Tooltip("この個数以上のノーツを生成したら、完了時に所要時間をログに出す")]
        [SerializeField] int generationLogNoteCount = 100;

        INoteSpawnDataOptionGetter optionHolder;
        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;

        // 自動更新の待ち状態
        bool isAutoRefreshRequested;
        float autoRefreshRequestedTime;
        float lastSlowRefreshLogTime = float.NegativeInfinity;

        // 生成の進み具合の表示
        readonly LogPanelNotice progressNotice = new LogPanelNotice(LogType.Log);
        bool wasGenerating;
        float generationStartTime;
        float nextProgressUpdateTime;
        int generationNoteCount;

        [Inject]
        public void Constructor(
            INoteSpawnDataOptionGetter optionHolder,
            IChartEditorDataGetter dataGetter,
            IChartEditorDataSetter dataSetter)
        {
            this.optionHolder = optionHolder;
            this.dataGetter = dataGetter;
            this.dataSetter = dataSetter;
        }

        private void Start()
        {
            // 編集操作はすべてUndo履歴を通るので、履歴の変化を「譜面が変わった」合図にする
            UndoRedo.History.OnChanged += RequestAutoRefresh;

            // ノートスピードが変わると、ノーツの並ぶ間隔が変わる。地面の進み方はすぐ変わるので、待たずに並べ直す
            // (ログは出さない。スライダーを動かすたびにログ欄が埋まってしまうため)
            if (optionHolder != null)
            {
                optionHolder.NoteSpeed
                    .Skip(1)
                    .Subscribe(_ => RefreshFromEditorData(false, false))
                    .AddTo(this.gameObject);

                // カーブの半径が変わると、ノーツの並ぶ曲線が変わるので、並べ直す (メニュー・クイックメニューから変えたとき)
                optionHolder.NoteCurveRadius
                    .Skip(1)
                    .Subscribe(_ => RefreshFromEditorData(false, false))
                    .AddTo(this.gameObject);
            }

            if (dataGetter == null) { return; }

            // オフセットはUndo履歴を通らないので個別に監視
            dataGetter.Offset
                .Skip(1)
                .Subscribe(_ => RequestAutoRefresh())
                .AddTo(this.gameObject);

            // インポートなどで譜面が丸ごと入れ替わったとき
            dataGetter.ChartData
                .Skip(1)
                .Subscribe(_ => RequestAutoRefresh())
                .AddTo(this.gameObject);
        }

        private void OnDestroy()
        {
            UndoRedo.History.OnChanged -= RequestAutoRefresh;
            progressNotice.Hide();
        }

        private void Update()
        {
            UpdateAutoRefresh();
            UpdateProgressNotice();
        }

        #region 自動更新

        /// <summary>
        /// 譜面が変わったので、少し待ってからプレビューに反映する
        /// </summary>
        private void RequestAutoRefresh()
        {
            isAutoRefreshRequested = true;
            autoRefreshRequestedTime = Time.unscaledTime;
        }

        private void UpdateAutoRefresh()
        {
            if (!isAutoRefreshRequested) { return; }

            if (!isAutoRefreshEnabled)
            {
                isAutoRefreshRequested = false;
                return;
            }

            // 譜面の読み込みが終わるまで待つ (読み込み途中の譜面をプレビューに出さない)
            if (dataGetter != null && dataGetter.IsLoadingChart.Value) { return; }
            // 続けて行われた編集が落ち着くまで待つ
            if (Time.unscaledTime - autoRefreshRequestedTime < autoRefreshDelaySeconds) { return; }
            // ドラッグの途中では反映しない
            if (Input.GetMouseButton(0)) { return; }

            RefreshFromEditorData(false, false);
        }

        #endregion

        #region 外部から呼ぶ処理

        /// <summary>
        /// 今の譜面をプレビューに反映する (変わったノーツだけ作り直す)
        /// </summary>
        public UniTask RefreshPreviewFromEditorDataAsync()
        {
            RefreshFromEditorData(false, true);
            return UniTask.CompletedTask;
        }

        /// <summary>
        /// 自動更新が待ち状態(編集してから反映までの短い間)なら、待たずに今すぐ反映する
        /// プレビューの上で操作を始める前に呼ぶ。古いプレビューを基準に位置を計算しないようにするため
        /// </summary>
        /// <returns>反映したとき true</returns>
        public bool FlushPendingRefresh()
        {
            if (!isAutoRefreshRequested || !isAutoRefreshEnabled) { return false; }
            if (dataGetter != null && dataGetter.IsLoadingChart.Value) { return false; }

            RefreshFromEditorData(false, false);
            return true;
        }

        /// <summary>
        /// 今の譜面を、待たずにすぐプレビューに反映する (プレビュー上でノーツをドラッグしている間の追従用)
        /// 自動更新と同じ扱いで、変換のログは出さない
        /// </summary>
        public void RefreshImmediately()
        {
            RefreshFromEditorData(false, false);
        }

        /// <summary>
        /// 今の譜面でプレビューをすべて作り直す
        /// </summary>
        public void RebuildPreviewFromEditorData()
        {
            RefreshFromEditorData(true, true);
        }

        /// <summary>
        /// 保存形式の譜面データをプレビューに反映する
        /// </summary>
        public void RefreshPreview(ChartDataOrigin savedChartData)
        {
            if (savedChartData == null)
            {
                Debug.LogWarning("[ChartPreviewRefresher] Saved chart data is null.");
                return;
            }

            if (!CanRefresh()) { return; }

            ChartImporterForRhythmGame importer = new ChartImporterForRhythmGame();
            global::ChartData chartData = importer.Import(savedChartData, optionHolder);

            ApplyToPreview(chartData, false);
        }

        #endregion

        #region 変換と反映

        /// <summary>
        /// 編集中の譜面を本編用のデータに変換して、プレビューに反映する
        /// </summary>
        /// <param name="isFullRebuild">true のとき、変わっていないノーツも含めてすべて作り直す</param>
        /// <param name="isManual">true のとき、ボタンなどからの明示的な更新 (変換のログを出す)</param>
        private void RefreshFromEditorData(bool isFullRebuild, bool isManual)
        {
            // これから最新の内容を反映するので、待っていた自動更新は不要
            isAutoRefreshRequested = false;

            var editorChartData = dataGetter?.ChartData?.Value;
            if (editorChartData == null) { return; }
            if (!CanRefresh()) { return; }

            var stopwatch = Stopwatch.StartNew();
            double exportMilliseconds;
            double importMilliseconds;
            global::ChartData chartData;

            try
            {
                // 自動更新では変換のログを出さない
                // (編集のたびに「変換成功」や、置いている途中のホールドの警告でログ欄が埋まってしまうため。
                //  置いている途中のノーツの問題は、赤いアウトラインとレギュレーション違反の表示で分かる)
                bool isLogEnabled = Debug.unityLogger.logEnabled;
                try
                {
                    if (!isManual) { Debug.unityLogger.logEnabled = false; }

                    // 編集中のデータ → 保存形式のデータ
                    ChartDataOrigin chartDataOrigin = new ChartExporter().Export(editorChartData, dataGetter.Offset.Value);
                    exportMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

                    // 保存形式のデータ → 本編用のデータ
                    chartData = new ChartImporterForRhythmGame().Import(chartDataOrigin, optionHolder);
                    importMilliseconds = stopwatch.Elapsed.TotalMilliseconds - exportMilliseconds;
                }
                finally
                {
                    Debug.unityLogger.logEnabled = isLogEnabled;
                }
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return;
            }

            stopwatch.Restart();
            var result = ApplyToPreview(chartData, isFullRebuild);
            double applyMilliseconds = stopwatch.Elapsed.TotalMilliseconds;

            LogRefreshTime(isManual, exportMilliseconds, importMilliseconds, applyMilliseconds, result);
        }

        private bool CanRefresh()
        {
            if (previewGenerator == null)
            {
                Debug.LogWarning("[ChartPreviewRefresher] Preview generator is not assigned.");
                return false;
            }

            if (optionHolder == null)
            {
                Debug.LogWarning("[ChartPreviewRefresher] Spawn option holder is not assigned.");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 本編用のデータをプレビューに反映する
        /// </summary>
        private ChartPreviewGenerator.ApplyResult ApplyToPreview(global::ChartData chartData, bool isFullRebuild)
        {
            // 表示用Meshを除き、実際に判定対象となるノーツ数をモデルへ反映する
            // (ノーツの生成を待たずに、変換が終わった時点で分かる)
            dataSetter?.SetTotalNoteCount(chartData?.MaxCombo ?? 0);

            if (groundController != null) { groundController.SetChartData(chartData); }

            return previewGenerator.Apply(chartData, isFullRebuild);
        }

        /// <summary>
        /// 反映にかかった時間をログに出す
        /// 手動の更新では毎回、自動更新では時間がかかったときだけ(間隔を空けて)出す
        /// </summary>
        private void LogRefreshTime(
            bool isManual,
            double exportMilliseconds,
            double importMilliseconds,
            double applyMilliseconds,
            ChartPreviewGenerator.ApplyResult result)
        {
            if (!isManual)
            {
                double total = exportMilliseconds + importMilliseconds + applyMilliseconds;
                if (total < slowRefreshLogMilliseconds) { return; }
                if (Time.unscaledTime - lastSlowRefreshLogTime < slowRefreshLogIntervalSeconds) { return; }

                lastSlowRefreshLogTime = Time.unscaledTime;
            }

            Debug.Log(
                $"【Preview】変換 {exportMilliseconds:F0}ms + {importMilliseconds:F0}ms / 差分 {applyMilliseconds:F0}ms / " +
                $"生成 {result.SpawnCount}個・破棄 {result.DiscardCount}個・そのまま {result.KeptCount}個" +
                (result.IsFullRebuild ? " (すべて作り直し)" : string.Empty));
        }

        #endregion

        #region 進み具合の表示

        /// <summary>
        /// ノーツの生成が長引いているとき、ログ欄に進み具合を表示する
        /// </summary>
        private void UpdateProgressNotice()
        {
            if (previewGenerator == null) { return; }

            bool isGenerating = previewGenerator.IsGenerating;
            float now = Time.unscaledTime;

            if (isGenerating)
            {
                if (!wasGenerating)
                {
                    generationStartTime = now;
                    generationNoteCount = 0;
                }

                generationNoteCount = Mathf.Max(generationNoteCount, previewGenerator.TotalSpawnCount);

                // すぐ終わる生成では表示しない (ちらつくだけなので)
                if (now - generationStartTime >= progressNoticeDelaySeconds && now >= nextProgressUpdateTime)
                {
                    nextProgressUpdateTime = now + 0.1f;

                    int total = previewGenerator.TotalSpawnCount;
                    int done = total - previewGenerator.RemainingSpawnCount;
                    progressNotice.Show($"プレビューのノーツを生成しています {done} / {total}");
                }
            }
            else if (wasGenerating)
            {
                progressNotice.Hide();

                if (generationNoteCount >= generationLogNoteCount)
                {
                    Debug.Log($"【Preview】ノーツ {generationNoteCount}個の生成が完了 ({now - generationStartTime:F1}秒)");
                }
            }

            wasGenerating = isGenerating;
        }

        #endregion
    }
}

using System;
using System.Diagnostics;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using JsonUtil;
using ChartConvert;
using static UndoRedo.History;
using Debug = UnityEngine.Debug;

namespace ChartEditor
{
    /// <summary>
    /// 譜面のファイルを読み込んで、編集中の譜面と入れ替える
    ///
    /// ・「開く」: ファイルを選んで読み込む。読み込んだファイルが、上書き保存の保存先になる
    /// ・「バックアップから復元」: 自動保存されたファイルや、上書き保存する前のファイルを読み込む。
    ///   バックアップのファイルそのものは、保存先にしない (上書き保存で、バックアップを書き換えてしまわないように)。
    ///   今開いている譜面のバックアップなら、保存先はそのまま。別の譜面のバックアップなら、保存先は未設定にする。
    ///   どちらも「保存されていない変更がある」状態になる (元のファイルは、保存するまで変わらない)。
    ///   復元する前の状態に、保存されていない変更があれば、それもバックアップに残してから入れ替える
    /// </summary>
    public class ChartDataImporter : MonoBehaviour
    {
        [Tooltip("読み込み中、1フレームのうち読み込みに使ってよい時間(ミリ秒)。大きいほど早く終わるが、画面がカクつく")]
        [SerializeField] float frameBudgetMilliseconds = 12f;

        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter editorDataSetter;
        ChartSaveGuard saveGuard;

        // 読み込みの進み具合の表示
        readonly LogPanelNotice progressNotice = new LogPanelNotice(LogType.Log);

        [Inject]
        public void Construct(IChartEditorDataGetter dataGetter, IChartEditorDataSetter editorDataSetter, ChartSaveGuard saveGuard)
        {
            this.dataGetter = dataGetter;
            this.editorDataSetter = editorDataSetter;
            this.saveGuard = saveGuard;
        }

        private void OnDestroy()
        {
            progressNotice.Hide();

            // 読み込みの途中でシーンが終わっても、保留の状態を次に持ち越さない
            ChainNoteTypeCorrection.Resume(false);
        }

        /// <summary>
        /// どこから読み込むか
        /// </summary>
        private enum Source
        {
            // ファイルを選んで開く
            FileDialog,
            // 決まったバックアップから復元する
            Backup,
            // バックアップのフォルダを開いて、ファイルを選んで復元する
            BackupDialog,
        }

        // バックアップを選ぶ画面で、選べるファイルの種類 (自動保存と、上書き保存する前のファイル)
        const string BACKUP_FILE_FILTER = "バックアップ (*.json;*.bak)|*.json;*.bak";

        public void Import()
        {
            // 読み込みの途中で、もう一度読み込みを始めない
            if (dataGetter.IsLoadingChart.Value) { return; }

            ImportAsync(Source.FileDialog, null, this.GetCancellationTokenOnDestroy()).Forget();
        }

        /// <summary>
        /// バックアップ (自動保存されたファイル、上書き保存する前のファイル) から、譜面を復元する
        /// </summary>
        public void ImportBackup(string backupPath)
        {
            if (dataGetter.IsLoadingChart.Value || string.IsNullOrEmpty(backupPath)) { return; }

            ImportAsync(Source.Backup, backupPath, this.GetCancellationTokenOnDestroy()).Forget();
        }

        /// <summary>
        /// 新しい譜面を作る (録音 20261117_051338)
        /// 保存していない変更があれば、別の譜面を開くときと同じように確かめる。保存先は未設定 (保存するときに選ぶ)。
        /// 楽曲はそのまま。オフセットは 0。延長の設定のとおりに、最初の小節を作る (extendFirstBars)
        /// </summary>
        public void CreateNewChart(Action extendFirstBars)
        {
            if (dataGetter.IsLoadingChart.Value) { return; }
            if (!saveGuard.ConfirmDiscardChanges("新しい譜面を作成")) { return; }

            editorDataSetter.SetChartData(new ChartData(0));
            editorDataSetter.SetOffset(0f);
            extendFirstBars?.Invoke();

            // 作った直後は、元に戻す履歴も、保存していない変更も無い
            ResetStates();
            ChartFilePathCache.CurrentChartFilePath = string.Empty;
            saveGuard.MarkSaved();
            Debug.Log("【譜面】新しい譜面を作りました。保存するときは、保存先を選んでください");
        }

        /// <summary>
        /// 自動保存のフォルダを開いて、選んだファイルから、譜面を復元する
        /// </summary>
        public void ImportBackupFromDialog()
        {
            if (dataGetter.IsLoadingChart.Value) { return; }

            ImportAsync(Source.BackupDialog, null, this.GetCancellationTokenOnDestroy()).Forget();
        }

        private async UniTaskVoid ImportAsync(Source source, string backupPath, CancellationToken token)
        {
            // 未保存の変更があれば確認
            // (バックアップからの復元で、今の内容がもうバックアップにあるときは、聞かない。
            //  復元した直後に、別のバックアップを試すときなど。今の内容へは、同じ一覧から戻れる)
            bool isRestore = source != Source.FileDialog;
            if (!isRestore || saveGuard.HasChangesSinceBackup)
            {
                if (!saveGuard.ConfirmDiscardChanges(isRestore ? "バックアップから復元" : "別の譜面を読み込み")) { return; }
            }

            ChartDataOrigin chartDataOrigin;
            string loadedPath;
            string loadError;
            bool isLoaded;
            switch (source)
            {
                case Source.Backup:
                    loadedPath = backupPath;
                    isLoaded = JsonLoader.TryLoadFromJsonFile(backupPath, out chartDataOrigin, out loadError);
                    break;
                case Source.BackupDialog:
                    isLoaded = JsonLoader.TryLoadFromJsonFileDialog(out chartDataOrigin, out loadedPath, out loadError, ChartSaveGuard.AutoSaveDirectoryPath, BACKUP_FILE_FILTER);
                    break;
                default:
                    // Load from file dialog and keep the path for subsequent Ctrl+S overwrite.
                    isLoaded = JsonLoader.TryLoadFromJsonFileDialog(out chartDataOrigin, out loadedPath, out loadError);
                    break;
            }

            if (!isLoaded)
            {
                // キャンセル時はエラー文が空。読み込みに失敗したときだけ通知する
                if (!string.IsNullOrEmpty(loadError))
                {
                    MessageDialog.ShowError("譜面を読み込めません", $"{loadError}\n\n現在の譜面はそのまま残しています。\n{loadedPath}");
                }
                return;
            }

            // 選んだファイルが、バックアップか (バックアップのフォルダを開いた画面から、ふつうの譜面を選ぶこともできる)
            bool isBackup = ChartBackups.IsBackupPath(loadedPath, ChartSaveGuard.AutoSaveDirectoryPath);

            // 現在の譜面を破棄する前に、読み込めるデータか検査する
            if (!ChartImporterForChartEditor.TryValidate(chartDataOrigin, out string validationError))
            {
                MessageDialog.ShowError("譜面を読み込めません", $"{validationError}\n\n現在の譜面はそのまま残しています。\n{loadedPath}");
                return;
            }

            // バックアップから復元するときは、復元する前の状態も、バックアップに残しておく
            // (選び間違えたときに、同じ一覧から戻せるように。保存されていない変更が無ければ、何も作らない)
            if (isBackup) { saveGuard.BackupNow(); }

            // 失敗したとき元に戻せるよう、現在の状態を退避
            ChartDataOrigin previousChart = ExportCurrentChart();
            float previousOffset = dataGetter.Offset.Value;

            bool isCompletelyImported;
            try
            {
                // 読み込みは数フレームに分けて行う。その間は編集を受け付けない
                // ホールドの種類の自動補正も、前後のつながりが出そろうまで保留する
                editorDataSetter.SetLoadingChart(true);
                ChainNoteTypeCorrection.Suspend();

                isCompletelyImported = await ImportToEditorAsync(chartDataOrigin, token);
            }
            catch (OperationCanceledException)
            {
                // シーンの終了などで中断された
                return;
            }
            catch (Exception ex)
            {
                Debug.LogError($"【Import】譜面の読み込みに失敗しました: {ex.Message}\n{ex.StackTrace}");
                RestoreChart(previousChart, previousOffset);
                MessageDialog.ShowError("譜面を読み込めません",
                    $"読み込み中にエラーが発生したため、元の譜面に戻しました。\n{loadedPath}\n\n{ex.Message}\n\n" +
                    "※接続していないホールドなど、保存できない状態のノーツは戻せていない可能性があります。\n" +
                    "　元のファイルを上書きしないよう、保存先は未設定にしています(保存時に保存先を選び直してください)。");
                return;
            }
            finally
            {
                progressNotice.Hide();

                // 保留していたホールドの種類の補正を、つながりが確定した状態でまとめて行う
                // (シーンの終了で中断されたときは、ノーツがもう無いので行わずに捨てる)
                bool isAlive = this != null;
                ChainNoteTypeCorrection.Resume(isAlive);
                if (isAlive) { editorDataSetter.SetLoadingChart(false); }
            }

            // Reset undo/redo history after import.
            ResetStates();

            if (isCompletelyImported && isBackup)
            {
                // バックアップのファイルそのものは、保存先にしない (上書き保存で、バックアップを書き換えてしまわないように)
                // 今開いていた譜面のバックアップなら、保存先はそのまま。別の譜面のバックアップなら、保存先を未設定にする
                if (!ChartBackups.IsBackupOf(loadedPath, ChartFilePathCache.CurrentChartFilePath, ChartSaveGuard.AUTO_SAVE_PREFIX))
                {
                    ChartFilePathCache.CurrentChartFilePath = string.Empty;
                }

                // 復元した内容は、まだ譜面のファイルには保存されていない (同じ内容がバックアップにあるので、自動保存はしない)
                saveGuard.MarkRestored();
                Debug.Log($"【復元】バックアップから譜面を復元しました。保存するまで、元のファイルは変わりません\n{loadedPath}");
            }
            else if (isCompletelyImported)
            {
                ChartFilePathCache.CurrentChartFilePath = loadedPath;
                saveGuard.MarkSaved();
            }
            else
            {
                // 一部のノーツを読み込めなかった譜面で、元のファイルを上書きさせない
                ChartFilePathCache.CurrentChartFilePath = string.Empty;
                saveGuard.MarkDirty();
                MessageDialog.ShowError("一部のノーツを読み込めませんでした",
                    $"このファイルには、エディタで読み込めないノーツが含まれていました。\n{loadedPath}\n\n" +
                    "読み込めた分だけを表示しています。元のファイルを上書きしないよう、保存先は未設定にしています\n" +
                    "(保存するときは保存先を選び直してください)。");
            }
        }

        /// <summary>
        /// 譜面を読み込む。小節ごとに進め、1フレームに使ってよい時間を超えたら次のフレームに続きを回す
        /// </summary>
        /// <returns>すべてのノーツを読み込めたとき true</returns>
        private async UniTask<bool> ImportToEditorAsync(ChartDataOrigin chartDataOrigin, CancellationToken token)
        {
            var stopwatch = Stopwatch.StartNew();
            var totalStopwatch = Stopwatch.StartNew();

            ChartImporterForChartEditor chartImporter = new ChartImporterForChartEditor();

            ChartData chartData = new ChartData(0);
            editorDataSetter.SetChartData(chartData);
            chartImporter.Begin(chartDataOrigin, chartData, editorDataSetter);

            int barCount = chartImporter.BarCount;
            for (int i = 0; i < barCount; i++)
            {
                chartImporter.ImportBar(i);

                if (stopwatch.Elapsed.TotalMilliseconds < frameBudgetMilliseconds) { continue; }

                // 続きは次のフレームで
                progressNotice.Show($"譜面を読み込んでいます {i + 1} / {barCount} 小節");
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                stopwatch.Restart();
            }

            bool isSucceed = chartImporter.End();
            Debug.Log($"【Import】{barCount}小節の読み込みが完了 ({totalStopwatch.Elapsed.TotalSeconds:F1}秒)");
            return isSucceed;
        }

        /// <summary>
        /// 譜面をまとめて読み込む (読み込み失敗時に元の譜面へ戻すとき用)
        /// </summary>
        private void ImportToEditor(ChartDataOrigin chartDataOrigin)
        {
            ChartImporterForChartEditor chartImporter = new ChartImporterForChartEditor();

            ChartData chartData = new ChartData(0);
            editorDataSetter.SetChartData(chartData);
            chartImporter.Import(chartDataOrigin, ref chartData, editorDataSetter);
        }

        /// <summary>
        /// 現在の譜面を退避用に変換する。譜面が無いときは null
        /// </summary>
        private ChartDataOrigin ExportCurrentChart()
        {
            var current = dataGetter.ChartData.Value;
            if (current == null) { return null; }

            try
            {
                return new ChartExporter().Export(current, dataGetter.Offset.Value);
            }
            catch (Exception ex)
            {
                Debug.LogError($"【Import】現在の譜面の退避に失敗しました: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 読み込み失敗時に元の譜面へ戻す
        /// </summary>
        private void RestoreChart(ChartDataOrigin previousChart, float previousOffset)
        {
            try
            {
                if (previousChart == null)
                {
                    editorDataSetter.SetChartData(new ChartData(0));
                    editorDataSetter.SetOffset(previousOffset);
                }
                else
                {
                    ImportToEditor(previousChart);
                }
            }
            catch (Exception ex)
            {
                // 復元にも失敗したときは、上書き保存で元ファイルを壊さないよう保存先を忘れる
                Debug.LogError($"【Import】元の譜面の復元に失敗しました: {ex.Message}\n{ex.StackTrace}");
                editorDataSetter.SetChartData(new ChartData(0));
            }

            // 作り直したオブジェクトと履歴が食い違うので履歴は破棄する
            ResetStates();

            // 退避 → 復元の往復では、保存できない状態のノーツ(未接続のホールド等)が戻らない
            // 欠けた譜面で元のファイルを上書きしないよう、保存先を忘れて未保存扱いにする
            ChartFilePathCache.CurrentChartFilePath = string.Empty;
            saveGuard.MarkDirty();
        }
    }
}

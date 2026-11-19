using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UniRx;
using VContainer.Unity;
using ChartConvert;
using JsonUtil;

namespace ChartEditor
{
    /// <summary>
    /// 譜面の未保存状態を管理する
    /// ・編集されたら未保存フラグを立てる
    /// ・未保存のまま終了/インポートしようとしたとき確認を出す
    /// ・一定時間ごとにバックアップを自動保存する
    ///   (バックアップからの復元は、メニューの「ファイル」から行う。一覧は ChartBackups、読み込みは ChartDataImporter)
    /// </summary>
    public class ChartSaveGuard : IStartable, ITickable, IDisposable
    {
        // 自動保存 (バックアップ) の間隔 (分)。メニューの「設定」で選び、次に起動したときも同じになる
        // 選べるのは 3 / 5 / 10 / 30 分と、なし (0: 自動では保存しない) (録音 20261117_162641)
        public const int DEFAULT_AUTO_SAVE_INTERVAL_MINUTES = 3;
        public static readonly int[] AUTO_SAVE_INTERVAL_CHOICES = { 3, 5, 10, 30, 0 };
        const string AUTO_SAVE_INTERVAL_PREFS_KEY = "ChartEditor.AutoSaveIntervalMinutes";
        static int autoSaveIntervalMinutes = -1;

        /// <summary>自動保存 (バックアップ) の間隔 (分)。0 のときは、自動では保存しない</summary>
        public static int AutoSaveIntervalMinutes
        {
            get
            {
                if (autoSaveIntervalMinutes < 0)
                {
                    int saved = PlayerPrefs.GetInt(AUTO_SAVE_INTERVAL_PREFS_KEY, DEFAULT_AUTO_SAVE_INTERVAL_MINUTES);
                    autoSaveIntervalMinutes = Array.IndexOf(AUTO_SAVE_INTERVAL_CHOICES, saved) >= 0 ? saved : DEFAULT_AUTO_SAVE_INTERVAL_MINUTES;
                }
                return autoSaveIntervalMinutes;
            }
            set
            {
                // 選べる値のほかは受け付けない
                if (Array.IndexOf(AUTO_SAVE_INTERVAL_CHOICES, value) < 0) { return; }
                autoSaveIntervalMinutes = value;
                PlayerPrefs.SetInt(AUTO_SAVE_INTERVAL_PREFS_KEY, autoSaveIntervalMinutes);
                PlayerPrefs.Save();
            }
        }
        // 残しておく自動保存の数 (これより古いものは消す)
        public const int AUTO_SAVE_KEEP_COUNT = 10;
        const string AUTO_SAVE_DIRECTORY = "ChartEditorAutoSave";
        // 自動保存のファイル名の先頭 (この後ろに、譜面の名前と日時が付く。ChartBackups が、この形から譜面の名前を読み取る)
        public const string AUTO_SAVE_PREFIX = "autosave_";

        readonly IChartEditorDataGetter dataGetter;

        IDisposable offsetSubscription;
        bool isDirtySinceAutoSave;
        float dirtyStartTime;

        /// <summary>
        /// 保存されていない変更があるか
        /// </summary>
        public bool IsDirty { get; private set; }

        /// <summary>
        /// 自動保存先のフォルダ
        /// </summary>
        public static string AutoSaveDirectoryPath => Path.Combine(Application.persistentDataPath, AUTO_SAVE_DIRECTORY);

        public ChartSaveGuard(IChartEditorDataGetter dataGetter)
        {
            this.dataGetter = dataGetter;
        }

        void IStartable.Start()
        {
            // 編集操作はすべてUndo履歴を通るので、履歴の変化を「変更あり」とみなす
            UndoRedo.History.OnChanged += MarkDirty;

            // オフセットはUndo履歴を通らないので個別に監視
            offsetSubscription = dataGetter.Offset
                .Skip(1)
                .Subscribe(_ => MarkDirty());

            Application.wantsToQuit += OnWantsToQuit;
        }

        void ITickable.Tick()
        {
            if (!isDirtySinceAutoSave) { return; }
            // 「なし」を選んでいるときは、自動では保存しない
            if (AutoSaveIntervalMinutes <= 0) { return; }
            if (Time.realtimeSinceStartup - dirtyStartTime < AutoSaveIntervalMinutes * 60f) { return; }

            // 譜面の読み込み中は、読み込み途中の譜面を保存してしまうので見送る
            if (dataGetter.IsLoadingChart.Value) { return; }
            // 再生中・ドラッグ中は処理落ちを避けるため見送る
            if (dataGetter.PlayMode.Value == PlayMode.Play) { return; }
            if (Input.GetMouseButton(0)) { return; }

            AutoSave();
        }

        void IDisposable.Dispose()
        {
            UndoRedo.History.OnChanged -= MarkDirty;
            Application.wantsToQuit -= OnWantsToQuit;
            offsetSubscription?.Dispose();
        }

        public void MarkDirty()
        {
            IsDirty = true;

            if (!isDirtySinceAutoSave)
            {
                isDirtySinceAutoSave = true;
                dirtyStartTime = Time.realtimeSinceStartup;
            }
        }

        /// <summary>
        /// 保存・読込が完了したときに呼ぶ
        /// </summary>
        public void MarkSaved()
        {
            IsDirty = false;
            isDirtySinceAutoSave = false;
        }

        /// <summary>
        /// バックアップから復元したときに呼ぶ
        /// 復元した内容は、譜面のファイルには保存されていない (未保存)。ただし、同じ内容がバックアップにあるので、
        /// 次に編集されるまで、自動保存はしない
        /// (ここで自動保存の対象にすると、バックアップを順に試すたびに、試したものの写しが増えて、古いバックアップが押し出されていく)
        /// </summary>
        public void MarkRestored()
        {
            IsDirty = true;
            isDirtySinceAutoSave = false;
        }

        /// <summary>
        /// 今の内容が、まだバックアップに無いか (最後に自動保存・復元・保存してから、編集されたか)
        /// </summary>
        public bool HasChangesSinceBackup => isDirtySinceAutoSave;

        /// <summary>
        /// 今の譜面を、すぐにバックアップへ保存する (今の内容が、もうバックアップにあるときは、何もしない)
        /// バックアップから復元する直前に呼ぶ。復元する前の状態も残しておけば、選び間違えたときに、同じ一覧から戻せる
        /// </summary>
        public void BackupNow()
        {
            if (!isDirtySinceAutoSave) { return; }

            AutoSave();
        }

        /// <summary>
        /// 未保存の変更があるとき、破棄してよいか確認する
        /// </summary>
        /// <returns>続行してよいとき true</returns>
        public bool ConfirmDiscardChanges(string operationName)
        {
            if (!IsDirty) { return true; }

            return MessageDialog.Confirm(
                "未保存の変更があります",
                $"保存されていない変更があります。\n保存せずに{operationName}しますか？\n\n「はい」: 変更を破棄して続ける\n「いいえ」: 編集に戻る");
        }

        private bool OnWantsToQuit()
        {
            return ConfirmDiscardChanges("終了");
        }

        /// <summary>
        /// アプリケーションを終了する (メニューの「ファイル → アプリケーションを終了」。録音 20261119_161332)
        /// 保存していない変更があるときは、ウィンドウの × で閉じるときと同じ確認を出し、「いいえ」なら編集に戻る
        /// </summary>
        public void RequestQuit()
        {
#if UNITY_EDITOR
            // Unity エディタでは Application.Quit が効かず、wantsToQuit も呼ばれないので、ここで確かめてから再生を止める
            if (!ConfirmDiscardChanges("終了")) { return; }
            UnityEditor.EditorApplication.isPlaying = false;
#else
            // 確認は、終了の求めを受けた OnWantsToQuit が出す (ここで確かめると、2回聞いてしまう)
            Application.Quit();
#endif
        }

        /// <summary>
        /// バックアップを自動保存する
        /// </summary>
        private void AutoSave()
        {
            // 成否にかかわらず、次の変更まで再試行しない
            isDirtySinceAutoSave = false;

            var chartData = dataGetter.ChartData.Value;
            if (chartData == null || chartData.BarDatas.Count == 0) { return; }

            try
            {
                var chartDataOrigin = new ChartExporter().Export(chartData, dataGetter.Offset.Value);

                // 名前には、譜面のあるフォルダの名前と、譜面の名前を入れる
                // (譜面のファイル名は、曲が違っても同じことが多い。フォルダの名前で、どの曲の譜面かを見分ける)
                string fileName = ChartBackups.CreateAutoSaveFileName(AUTO_SAVE_PREFIX, ChartFilePathCache.CurrentChartFilePath, DateTime.Now);
                string path = Path.Combine(AutoSaveDirectoryPath, fileName);

                if (JsonWriter.TrySaveToJsonPath(chartDataOrigin, path))
                {
                    DeleteOldAutoSaves();
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"【AutoSave】自動保存に失敗しました: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// 古い自動保存ファイルを削除する
        /// </summary>
        private void DeleteOldAutoSaves()
        {
            var oldFiles = new DirectoryInfo(AutoSaveDirectoryPath)
                .GetFiles($"{AUTO_SAVE_PREFIX}*.json")
                .OrderByDescending(file => file.LastWriteTimeUtc)
                .Skip(AUTO_SAVE_KEEP_COUNT);

            foreach (var file in oldFiles)
            {
                file.Delete();
            }
        }
    }
}

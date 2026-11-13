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
    /// </summary>
    public class ChartSaveGuard : IStartable, ITickable, IDisposable
    {
        const float AUTO_SAVE_INTERVAL_SECONDS = 180f;
        const int AUTO_SAVE_KEEP_COUNT = 10;
        const string AUTO_SAVE_DIRECTORY = "ChartEditorAutoSave";
        const string AUTO_SAVE_PREFIX = "autosave_";

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
            if (Time.realtimeSinceStartup - dirtyStartTime < AUTO_SAVE_INTERVAL_SECONDS) { return; }

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

                string chartName = string.IsNullOrWhiteSpace(ChartFilePathCache.CurrentChartFilePath)
                    ? "untitled"
                    : Path.GetFileNameWithoutExtension(ChartFilePathCache.CurrentChartFilePath);
                string fileName = $"{AUTO_SAVE_PREFIX}{chartName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
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

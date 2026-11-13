using UnityEngine;

namespace ChartEditor
{
    /// <summary>
    /// 画面のログ欄に、時間がたっても消えないお知らせを1件表示する
    /// (通常のログは数秒で消えるので、状態を出し続けたいときに使う)
    /// </summary>
    public class LogPanelNotice
    {
        readonly LogType logType;

        LogFactoryView logFactoryView;
        bool hasSearchedLogView;
        LogItem logItem;

        /// <param name="logType">背景色を決める種類 (Log: 黒 / Warning: 黄 / Error: 赤)</param>
        public LogPanelNotice(LogType logType)
        {
            this.logType = logType;
        }

        /// <summary>
        /// 表示中か
        /// </summary>
        public bool IsShowing => logItem != null;

        /// <summary>
        /// お知らせを表示する。すでに表示中なら文章だけ差し替える
        /// </summary>
        public void Show(string text)
        {
            if (logItem != null)
            {
                logItem.SetText(text);
                return;
            }

            // ログ欄を探す (探すのは最初の1回だけ。シーンに無ければ何もしない)
            if (!hasSearchedLogView)
            {
                hasSearchedLogView = true;
                logFactoryView = Object.FindObjectOfType<LogFactoryView>(true);
                if (logFactoryView == null) { Debug.LogWarning("【LogPanel】ログ欄(LogFactoryView)がシーンに無いため、お知らせを表示できません"); }
            }
            if (logFactoryView == null) { return; }

            logItem = logFactoryView.Spawn(new LogData
            {
                Time = Time.unscaledTime,
                LogType = logType,
                Logtext = text,
            });

            // 消すまで表示し続ける
            logItem.KeepDisplaying();
        }

        /// <summary>
        /// お知らせを消す
        /// </summary>
        public void Hide()
        {
            if (logItem == null) { return; }

            Object.Destroy(logItem.gameObject);
            logItem = null;
        }

        /// <summary>
        /// ほかのログが増えても、一番上に表示されるようにする
        /// </summary>
        public void KeepOnTop()
        {
            if (logItem == null) { return; }
            if (logItem.transform.GetSiblingIndex() == 0) { return; }

            logItem.transform.SetAsFirstSibling();
        }
    }
}

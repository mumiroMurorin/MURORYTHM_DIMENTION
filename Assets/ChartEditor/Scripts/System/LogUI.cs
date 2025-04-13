using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class LogUI : LocalSingletonMonoBehaviour<LogUI>
    {
        [SerializeField] LogFactoryView logFactory;

        public void Log(string str)
        {
            Debug.Log(str);

            LogData data = new LogData()
            {
                Logtext = str,
                LogType = LogType.General
            };

            logFactory.Spawn(data);
        }

        public void LogError(string str)
        {
            Debug.LogError(str);

            LogData data = new LogData()
            {
                Logtext = str,
                LogType = LogType.Error
            };

            logFactory.Spawn(data);
        }

        public void LogWarning(string str)
        {
            Debug.LogWarning(str);

            LogData data = new LogData()
            {
                Logtext = str,
                LogType = LogType.Warning
            };

            logFactory.Spawn(data);
        }
    }

}

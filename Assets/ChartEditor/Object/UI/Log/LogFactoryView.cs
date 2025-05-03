using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class LogFactoryView : MonoBehaviour
    {
        [SerializeField] LogGetter logGetter;

        [SerializeField] GameObject logPrefab;
        [SerializeField] Transform logParent;

        private void Start()
        {
            logGetter.OnReceiveLogListener += OnRecieveLog;
        }

        private void OnRecieveLog(LogData logData)
        {
            Spawn(logData);
        }

        /// <summary>
        /// ÉçÉOÇÃê∂ê¨
        /// </summary>
        /// <param name="logData"></param>
        public void Spawn(LogData logData)
        {
            GameObject obj = Instantiate(logPrefab);
            LogItem item = obj.GetComponent<LogItem>();

            obj.transform.SetParent(logParent);
            obj.transform.SetAsFirstSibling();

            item.SetLogData(logData);
        }
    }
}

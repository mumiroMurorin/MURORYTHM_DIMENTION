using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class LogFactoryView : MonoBehaviour
    {
        [SerializeField] GameObject logPrefab;
        [SerializeField] Transform logParent;
        
        /// <summary>
        /// ÉçÉOÇÃê∂ê¨
        /// </summary>
        /// <param name="logData"></param>
        public void Spawn(LogData logData)
        {
            GameObject obj = Instantiate(logPrefab);
            LogItem item = obj.GetComponent<LogItem>();

            obj.transform.SetParent(logParent);

            item.SetLogData(logData);
        }
    }
}

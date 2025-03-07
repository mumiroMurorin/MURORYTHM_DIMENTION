using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace ChartEditor
{
    public class BarLine : MonoBehaviour
    {
        [SerializeField] TextMeshPro[] numberTmps;

        /// <summary>
        /// 小節番号の設定
        /// </summary>
        /// <param name="number"></param>
        public void SetBarNumber(int number)
        {
            foreach(TextMeshPro tmp in numberTmps)
            {
                tmp.text = number.ToString();
            }
        } 
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace UIInSelectScene
{
    public class OptionTopicView_DivisionNum : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI divisionNumTmp;

        public void OnChangeDivisionNum(int divNum)
        {
            divisionNumTmp.text = divNum.ToString("0•ªŠ„");
        }
    }
}

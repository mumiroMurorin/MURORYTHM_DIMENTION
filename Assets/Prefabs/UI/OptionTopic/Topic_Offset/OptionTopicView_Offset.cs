using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace UIInSelectScene
{
    public class OptionTopicView_Offset : OptionTopicViewBase
    {
        [SerializeField] TextMeshProUGUI offsetTmp;

        public void OnChangeOffset(int offset)
        {
            string str = "";
            float fontSize = offsetTmp.fontSize;

            // 正の数のとき+を頭につける
            if (offset > 0) { str = $"<size={fontSize / 1.5f}>+</size>" + offset.ToString(); }
            else if (offset < 0) { str = $"<size={fontSize / 1.5f}>-</size>" + Mathf.Abs(offset).ToString(); }
            else { str = $"<size={fontSize / 1.5f}>±</size={fontSize}>0"; }

            offsetTmp.text = str;
        }
    }
}

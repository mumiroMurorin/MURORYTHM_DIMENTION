using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using UnityEngine.EventSystems;

namespace ChartEditor
{
    public class ChangeLaneDivNumButtonView : ButtonView
    {
        [SerializeField] TextMeshProUGUI tmp;

        public void OnLaneDivNumChanged(int divNum)
        {
            tmp.text = divNum.ToString();
        }
    }
}

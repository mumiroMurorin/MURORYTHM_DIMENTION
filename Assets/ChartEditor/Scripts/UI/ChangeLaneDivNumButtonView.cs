using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;

namespace ChartEditor
{
    public class ChangeLaneDivNumButtonView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI tmp;

        public Action OnButtonClickedListener;

        public void OnLaneDivNumChanged(int divNum)
        {
            tmp.text = divNum.ToString();
        }

        public void OnButtonClicked()
        {
            OnButtonClickedListener?.Invoke();
        }
    }

}

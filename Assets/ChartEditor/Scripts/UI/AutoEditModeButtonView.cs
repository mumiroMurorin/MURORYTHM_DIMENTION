using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChartEditor
{
    public class AutoEditModeButtonView : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] TextMeshProUGUI editTmp;

        [SerializeField, TextArea(1, 2)] string textOnEnable;
        [SerializeField, TextArea(1, 2)] string textOnDisable;

        public System.Action OnClickedListner { get; set; }

        public void OnChangeAutoEditMode(bool isEnable)
        {
            if (isEnable)
            {
                editTmp.text = textOnEnable;
            }
            else
            {
                editTmp.text = textOnDisable;
            }
        }

        public void OnClicked()
        {
            OnClickedListner.Invoke();
        }

    }

}

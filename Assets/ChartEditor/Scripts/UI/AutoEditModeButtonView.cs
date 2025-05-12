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
        [SerializeField] Image backGround;
        [SerializeField] TextMeshProUGUI editTmp;

        [SerializeField, TextArea(1, 2)] string textOnEnable;
        [SerializeField, TextArea(1, 2)] string textOnDisable;
        [SerializeField] Color colorAutoModing = Color.blue;

        Color defaultColor = Color.white;

        private void Start()
        {
            if (backGround) { defaultColor = backGround.color; }
        }

        public System.Action OnClickedListner { get; set; }

        public void OnChangeAutoEditMode(bool isEnable)
        {
            if (isEnable)
            {
                editTmp.text = textOnEnable;
                backGround.color = colorAutoModing;
            }
            else
            {
                editTmp.text = textOnDisable;
                backGround.color = defaultColor;
            }
        }

        public void OnClicked()
        {
            OnClickedListner.Invoke();
        }

    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChartEditor
{
    public class OffsetInputFieldView : MonoBehaviour
    {
        [SerializeField] TMP_InputField inputField;

        public System.Action<float> OnValueChangedListner { get; set; }

        public void OnChangeMainBPM(float offset)
        {
            inputField.text = offset.ToString();
        }

        public void OnChangePlayMode(PlayMode playMode)
        {
            inputField.interactable = playMode != PlayMode.Play;
        }

        public void OnValueChanged(string str)
        {
            if(!float.TryParse(str,out float offset)) { return; }
            OnValueChangedListner.Invoke(offset);
        }
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChartEditor
{
    public class BPMInputFieldView : MonoBehaviour
    {
        [SerializeField] TMP_InputField inputField;

        public System.Action<float> OnValueChangedListner { get; set; }

        public void OnChangeMainBPM(float bpm)
        {
            inputField.text = bpm.ToString();
        }

        public void OnChangePlayMode(PlayMode playMode)
        {
            inputField.interactable = playMode != PlayMode.Play;
        }

        public void OnValueChanged(string str)
        {
            if(!float.TryParse(str,out float bpm)) { return; }
            OnValueChangedListner.Invoke(bpm);
        }
    }
}


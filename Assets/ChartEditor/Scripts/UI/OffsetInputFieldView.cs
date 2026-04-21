using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChartEditor
{
    public class OffsetInputFieldView : InputFieldView
    {
        public void OnChangePlayMode(PlayMode playMode)
        {
            inputField.interactable = playMode != PlayMode.Play;
        }
    }
}


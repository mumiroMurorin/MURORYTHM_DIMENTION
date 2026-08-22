using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace UIInSelectScene
{
    public class OptionTopicView_NoteSpeed : OptionTopicViewBase
    {
        [SerializeField] TextMeshProUGUI noteSpeedTmp;

        public void OnChangeSpeed(float speed)
        {
            noteSpeedTmp.text = speed.ToString("0.0");
        }
    }
}

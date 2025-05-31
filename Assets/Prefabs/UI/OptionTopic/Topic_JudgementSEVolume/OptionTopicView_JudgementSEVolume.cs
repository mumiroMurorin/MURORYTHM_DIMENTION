using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace UIInSelectScene
{
    public class OptionTopicView_JudgementSEVolume : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI volumeTmp;

        public void OnChangeVolume(float vol)
        {
            volumeTmp.text = vol.ToString();
        }
    }
}

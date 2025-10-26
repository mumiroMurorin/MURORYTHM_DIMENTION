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
        [SerializeField] GameObject muteImage;
        [SerializeField] GameObject speakerImage;

        public void OnChangeVolume(float vol)
        {
            volumeTmp.text = vol.ToString();

            muteImage?.SetActive(Mathf.Approximately(vol, 0));
            speakerImage?.SetActive(!Mathf.Approximately(vol, 0));
        }
    }
}

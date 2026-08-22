using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace UIInSelectScene
{
    public class OptionTopicView_FastLate : OptionTopicViewBase
    {
        [SerializeField] TextMeshProUGUI tmp;
        [SerializeField] GameObject normalSample;
        [SerializeField] GameObject fastLateSample;

        public void OnChangeEnabledFastLate(string display)
        {
            tmp.text = display;
        }

        public void OnChangeEnabledFastLate(bool isEnabled)
        {
            normalSample.SetActive(!isEnabled);
            fastLateSample.SetActive(isEnabled);
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace UIInSelectScene
{
    public class OptionTopicView_FastLate : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI tmp;

        public void OnChangeEnabledFastLate(string display)
        {
            tmp.text = display;
        }
    }
}

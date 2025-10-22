using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace UIInSelectScene
{
    public class OptionTopicView_InfoSub : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI tmp;

        public void OnChangeSubInfo(string display)
        {
            tmp.text = display;
        }
    }
}

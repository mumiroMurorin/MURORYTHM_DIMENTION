using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace UIInSelectScene
{
    public class OptionTopicView_InfoMain : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI tmp;

        public void OnChangeMainInfo(string display)
        {
            tmp.text = display;
        }
    }
}

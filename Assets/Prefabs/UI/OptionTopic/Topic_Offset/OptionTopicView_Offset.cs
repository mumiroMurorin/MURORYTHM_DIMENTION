using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace UIInSelectScene
{
    public class OptionTopicView_Offset : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI offsetTmp;

        public void OnChangeOffset(int offset)
        {
            offsetTmp.text = offset.ToString();
        }
    }
}

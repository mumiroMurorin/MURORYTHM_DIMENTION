using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace UIInResultScene
{
    public class BreakdownView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI perfectCountText;
        [SerializeField] TextMeshProUGUI greatCountText;
        [SerializeField] TextMeshProUGUI goodCountText;
        [SerializeField] TextMeshProUGUI missCountText;

        public void OnChangePerfectCount(int count)
        {
            if(perfectCountText == null) { return; }

            perfectCountText.text = count.ToString();
        }

        public void OnChangeGreatCount(int count)
        {
            if (greatCountText == null) { return; }

            greatCountText.text = count.ToString();
        }

        public void OnChangeGoodCount(int count)
        {
            if (goodCountText == null) { return; }

            goodCountText.text = count.ToString();
        }

        public void OnChangeMissCount(int count)
        {
            if (missCountText == null) { return; }

            missCountText.text = count.ToString();
        }
    }

}

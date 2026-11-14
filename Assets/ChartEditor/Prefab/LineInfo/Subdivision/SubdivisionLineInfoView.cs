using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UniRx;

namespace ChartEditor
{
    public class SubdivisionLineInfoView : MonoBehaviour
    {
        [Header("BPM")]
        [SerializeField] GameObject bpmObject;
        [SerializeField] TextMeshPro bpmTmp;

        [Header("SpeedRatio")]
        [SerializeField] GameObject speedRatioObject;
        [SerializeField] TextMeshPro speedRatioTmp;

        /// <summary>
        /// BPM情報をUIにセット
        /// </summary>
        /// <param name="isDiffBack">1つ前の線とBPMが違うか。同じときは表記を出さない</param>
        public void SetBPM(float bpm, bool isDiffBack)
        {
            if (bpmTmp == null) { return; }

            // 同じ数字がずらっと並ぶと、BPMの変わり目が分かりにくいので、変わり目の線にだけ出す
            // (クリックを受ける当たり判定は別のオブジェクトにあるので、表記を隠した線でも、クリックして設定を開ける)
            bpmTmp.text = !isDiffBack ? "" : bpm.ToString();
            bpmObject.SetActive(isDiffBack);
        }

        /// <summary>
        /// スピード倍率をUIにセット
        /// </summary>
        /// <param name="speedRatio"></param>
        public void SetSpeedRatio(float speedRatio, bool isDiffBack)
        {
            if (speedRatioTmp == null) { return; }

            speedRatioTmp.text = !isDiffBack ? "" : "×<size=0.15>" + speedRatio.ToString();
            speedRatioObject.SetActive(isDiffBack);
        }
    }

}

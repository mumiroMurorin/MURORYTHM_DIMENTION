using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class SliderUnitsControllerView : MonoBehaviour
    {
        [Header("順にスライダーUIをアタッチ")]
        [SerializeField] SliderUnitUI[] sliderUnits;
        [Header("通常時色")]
        [SerializeField] Color nonActionColor;
        [Header("タッチ時補正")]
        [SerializeField] float pressedDecrmentionColorValue;
        [Header("タッチ時色変更時間")]
        [SerializeField] float interactSliderSeconds;

        /// <summary>
        /// 操作が追加された時の処理
        /// </summary>
        /// <param name="sliderTouchData"></param>
        public void OnChangeSliderData(SliderTouchData sliderTouchData)
        {
            foreach(int i in sliderTouchData.SliderIndices)
            {
                if (sliderUnits.Length <= i) {
                    Debug.LogError($"【UI】スライダーUIがアタッチされていません。 index:{i}");
                    return;
                }

                sliderUnits[i].SetSliderData(sliderTouchData);
            }
        }

        /// <summary>
        /// 操作が一新された時の処理
        /// </summary>
        public void OnClearSliderData()
        {
            foreach(var sliderUnit in sliderUnits)
            {
                sliderUnit.SetSliderData(new SliderTouchData(new int[0], null, nonActionColor));
            }
        }

        /// <summary>
        /// タッチされた時の処理
        /// </summary>
        /// <param name="sliderTouchData"></param>
        public void OnTouchSlider(SliderTouchData sliderTouchData)
        {
            foreach (int i in sliderTouchData.SliderIndices)
            {
                if (sliderUnits.Length <= i)
                {
                    Debug.LogError($"【UI】スライダーUIがアタッチされていません。 index:{i}");
                    return;
                }

                sliderUnits[i].InteractSlider(sliderTouchData, pressedDecrmentionColorValue, interactSliderSeconds);
            }
        }
    }

}

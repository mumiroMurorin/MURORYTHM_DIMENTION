using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring
{
    public class SliderInputHandler : MonoBehaviour
    {
        ISliderInputSetter sliderInputSetter;

        [Inject]
        public void Inject(ISliderInputSetter inputSetter)
        {
            sliderInputSetter = inputSetter;
        }

        // スライダー(キーボード) → ゲーム内入力
        Dictionary<KeyCode, int> keyCodeToSliderIndex = new Dictionary<KeyCode, int>
        {
            {KeyCode.X , 0},
            {KeyCode.W , 1},
            {KeyCode.V , 2},
            {KeyCode.U , 3},
            {KeyCode.M , 4},
            {KeyCode.N , 5},
            {KeyCode.O , 6},
            {KeyCode.P , 7},
            {KeyCode.L , 8},
            {KeyCode.K , 9},
            {KeyCode.J , 10},
            {KeyCode.I , 11},
            {KeyCode.A , 12},
            {KeyCode.B , 13},
            {KeyCode.C , 14},
            {KeyCode.D , 15},
        };

        void Update()
        {
            // 全てのキー入力を監視
            foreach(var pair in keyCodeToSliderIndex)
            {
                sliderInputSetter?.SetSliderInput(pair.Value, Input.GetKey(pair.Key));
            }
        }

        private void OnDestroy()
        {
            sliderInputSetter?.Dispose();
        }
    }

}

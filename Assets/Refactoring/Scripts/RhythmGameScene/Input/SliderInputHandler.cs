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
            {KeyCode.X , 7},
            {KeyCode.W , 6},
            {KeyCode.V , 5},
            {KeyCode.U , 4},
            {KeyCode.M , 3},
            {KeyCode.N , 2},
            {KeyCode.O , 1},
            {KeyCode.P , 0},
            {KeyCode.L , 15},
            {KeyCode.K , 14},
            {KeyCode.J , 13},
            {KeyCode.I , 12},
            {KeyCode.A , 11},
            {KeyCode.B , 10},
            {KeyCode.C , 9},
            {KeyCode.D , 8},
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

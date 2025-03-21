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
            {KeyCode.P , 0},
            {KeyCode.O , 1},
            {KeyCode.N , 2},
            {KeyCode.M , 3},
            {KeyCode.U , 4},
            {KeyCode.V , 5},
            {KeyCode.W , 6},
            {KeyCode.X , 7},
            {KeyCode.D , 8},
            {KeyCode.C , 9},
            {KeyCode.B , 10},
            {KeyCode.A , 11},
            {KeyCode.I , 12},
            {KeyCode.J , 13},
            {KeyCode.K , 14},
            {KeyCode.L , 15},
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace Refactoring
{
    public class SliderInputHandler : MonoBehaviour
    {

        [Header("スライダーからの入力(必ず長さ16に)")]
        [SerializeField] KeyCode[] keyCodes;

        ISliderInputSetter sliderInputSetter;

        // スライダー(キーボード) → ゲーム内入力
        Dictionary<KeyCode, int> keyCodeToSliderIndex = new Dictionary<KeyCode, int>
        {
            {KeyCode.A , 0},
            {KeyCode.B , 1},
            {KeyCode.C , 2},
            {KeyCode.D , 3},
            {KeyCode.E , 4},
            {KeyCode.F , 5},
            {KeyCode.G , 6},
            {KeyCode.H , 7},
            {KeyCode.I , 8},
            {KeyCode.J , 9},
            {KeyCode.K , 10},
            {KeyCode.L , 11},
            {KeyCode.M , 12},
            {KeyCode.N , 13},
            {KeyCode.O , 14},
            {KeyCode.P , 15},
        };

        [Inject]
        public void Inject(ISliderInputSetter inputSetter)
        {
            sliderInputSetter = inputSetter;
        }

        private void Start()
        {
            CheckSliderKeyCodes();
        }

        void Update()
        {
            // 全てのキー入力を監視
            foreach(var pair in keyCodeToSliderIndex)
            {
                sliderInputSetter?.SetSliderInput(pair.Value, Input.GetKey(pair.Key));
            }
        }

        private void CheckSliderKeyCodes()
        {
            if (keyCodes == null) { return; }
            if (keyCodes.Length != 16) { return; }

            keyCodeToSliderIndex.Clear();

            for(int i = 0;i<16;i++)
            {
                keyCodeToSliderIndex.Add(keyCodes[i], i);
            }

        }

        private void OnDestroy()
        {
            sliderInputSetter?.Dispose();
        }
    }

}

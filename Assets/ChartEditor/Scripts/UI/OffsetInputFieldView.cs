using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChartEditor
{
    public class OffsetInputFieldView : InputFieldView
    {
        void Awake()
        {
            // 欄の名前を左右にドラッグしても、値を動かせる (1ピクセルで 1 ms。入力を終えたときと同じく、欄の onEndEdit で反映する。
            // 録音 20261118_201544: クイックメニューのオフセットだけ、付いていなかった)
            NumberScrubHandle.AttachToField(inputField, NumberScrubHandle.Settings.Float(1f), true);
        }

        public void OnChangePlayMode(PlayMode playMode)
        {
            inputField.interactable = playMode != PlayMode.Play;
        }
    }
}


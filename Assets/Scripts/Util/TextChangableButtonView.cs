using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TextChangableButtonView : ButtonView
{
    [SerializeField] TextMeshProUGUI buttonText;
    [SerializeField] string textOnEnable;
    [SerializeField] string textOnDisable;

    public void OnChangeValue(bool value)
    {
        if (buttonText == null) { return; }

        buttonText.text = value ? textOnEnable : textOnDisable;
    }

}

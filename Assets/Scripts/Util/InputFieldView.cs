using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InputFieldView : MonoBehaviour
{
    [SerializeField] protected TMP_InputField inputField;

    public event System.Action<float> OnFloatValueChangedListner;
    public event System.Action<int> OnIntValueChangedListner;
    public event System.Action<string> OnStringValueChangedListner;


    public virtual void OnChangeFloatValue(float f)
    {
        inputField.text = f.ToString();
    }

    public virtual void OnChangeIntValue(int i)
    {
        inputField.text = i.ToString();
    }

    public virtual void OnChangeStringValue(string str)
    {
        inputField.text = str;
    }


    public virtual void OnFloatFieldValueChanged(string str)
    {
        if (!float.TryParse(str, out float f)) { return; }
        OnFloatValueChangedListner?.Invoke(f);
    }

    public virtual void OnIntFieldValueChanged(string str)
    {
        if (!int.TryParse(str, out int i)) { return; }
        OnIntValueChangedListner?.Invoke(i);
    }

    public virtual void OnStringFieldValueChanged(string str)
    {
        OnStringValueChangedListner?.Invoke(str);
    }
}

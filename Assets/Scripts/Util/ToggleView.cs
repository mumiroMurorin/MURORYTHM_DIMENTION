using UnityEngine;
using UnityEngine.UI;
using System;

[RequireComponent(typeof(Toggle))]
public class ToggleView : MonoBehaviour
{
    Toggle toggle;

    public Action<bool> OnPushToggleListner;

    void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnPushToggle);
    }

    public void OnPushToggle(bool isOn)
    {
        OnPushToggleListner?.Invoke(isOn);
        UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
    }

    public void OnChangeModelValue(bool isActive)
    {
        toggle.isOn = isActive;
    }
}

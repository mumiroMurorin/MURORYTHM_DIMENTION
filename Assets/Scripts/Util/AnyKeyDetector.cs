using UnityEngine;
using System;

public class AnyKeyDetector : MonoBehaviour
{
    private string[] lastNames = Array.Empty<string>();

    private void Start()
    {
        Debug.Log($"{string.Join(",", Input.GetJoystickNames())}");
    }

    void Update()
    {
        if (Input.anyKeyDown)
        {
            foreach (KeyCode keyCode in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(keyCode))
                {
                    Debug.Log($"âüÇ≥ÇÍÇΩÉLÅ[: {keyCode}");
                }
            }
        }
    }

    void DebugJoystick()
    {
        var names = Input.GetJoystickNames();

        if (!SameNames(lastNames, names))
        {
            Debug.Log("Joystick names:");
            for (int i = 0; i < names.Length; i++)
            {
                Debug.Log($"{i + 1}: '{names[i]}'");
            }
            lastNames = names;
        }

        for (int joy = 1; joy <= 8; joy++)
        {
            for (int btn = 0; btn < 12; btn++)
            {
                var keyName = $"Joystick{joy}Button{btn}";
                if (Enum.TryParse(keyName, out KeyCode keyCode) && Input.GetKeyDown(keyCode))
                {
                    Debug.Log($"Pressed: {keyName}");
                }
            }
        }
    }

    private bool SameNames(string[] a, string[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i]) return false;
        }
        return true;
    }

    void Check(KeyCode key)
    {
        if (Input.GetKeyDown(key))
        {
            Debug.Log(key.ToString());
        }
    }
}
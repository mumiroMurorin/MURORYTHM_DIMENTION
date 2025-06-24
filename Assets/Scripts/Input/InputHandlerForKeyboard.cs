using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class InputHandlerForKeyboard : MonoBehaviour
{
    void Update()
    {
        // ゲームの強制終了
        if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Escape)) { QuitGame(); }

        EachUpdate();
    }

    protected abstract void EachUpdate();

    /// <summary>
    /// ゲームの強制終了
    /// </summary>
    private void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
    }
}

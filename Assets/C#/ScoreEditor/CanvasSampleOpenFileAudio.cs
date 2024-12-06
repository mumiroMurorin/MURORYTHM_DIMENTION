using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
//using SFB;

[RequireComponent(typeof(Button))]
public class CanvasSampleOpenFileAudio : MonoBehaviour, IPointerDownHandler {
   
    [SerializeField] private EditorCtrl editorCtrl;
    private string musicURL;

#if UNITY_WEBGL && !UNITY_EDITOR
    //
    // WebGL
    //
    [DllImport("__Internal")]
    private static extern void UploadFile(string gameObjectName, string methodName, string filter, bool multiple);

    public void OnPointerDown(PointerEventData eventData) {
        UploadFile(gameObject.name, "OnFileUpload", ".png, .jpg", false);
    }

    // Called from browser
    public void OnFileUpload(string url) {
        StartCoroutine(OutputRoutine(url));
    }
#else
    //
    // Standalone platforms & editor
    //
    public void OnPointerDown(PointerEventData eventData) { }

    void Start() {
        var button = GetComponent<Button>();
        button.onClick.AddListener(OnClick);
    }

    private void OnClick() {
        //var paths = StandaloneFileBrowser.OpenFilePanel("‰¹Šy‚ð‘I‘ð", "", "mp3", false);
        //if (paths.Length > 0) {
            //StartCoroutine(OutputRoutine(new System.Uri(paths[0]).AbsoluteUri));
        //}
    }
#endif

    /*
    private IEnumerator OutputRoutine(string url) {
        var loader = new WWW(url);
        yield return loader;
        musicURL = url;
        string[] part = url.Split('/');
        //editorCtrl.RegisterAudioClip(loader.GetAudioClip(), WWW.UnEscapeURL(part[part.Length - 1]));
    }*/
}
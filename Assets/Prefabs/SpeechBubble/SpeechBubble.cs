using UnityEngine;
using TMPro;

public abstract class SpeechBubble : MonoBehaviour
{
    [SerializeField] protected TextMeshProUGUI tmp;

    public void Speak(string text, SpeechBubbleConfig config)
    {
        // this.tmp.text = text;
        // this.gameObject.SetActive(true);

        OnSpeak(text, config);
    }

    protected abstract void OnSpeak(string text, SpeechBubbleConfig config);

    public void ShutUp()
    {
        if (this == null) { return; }
        if (this.gameObject == null) { return; }

        // this?.gameObject?.SetActive(false);

        OnShutUp();
    }

    protected abstract void OnShutUp();
}

[System.Serializable]
public class SpeechBubbleConfig
{
    [Tooltip("0未満のときデフォルトの値になる")]
    [SerializeField] float fontSize = -1f;
    [SerializeField] Color fontColor = Color.white;
    [Tooltip("文字出現スピード[char/sec]")]
    [SerializeField] float characterRevealSpeed = 10f;
    [Tooltip("感情")]
    [SerializeField] FaceEmotion emotion = FaceEmotion.Normal;

    public float FontSize { get { return fontSize; } set { fontSize = value; } }

    public Color FontColor { get { return fontColor; } set { fontColor = value; } }

    public float CharacterRevealSpeed { get { return characterRevealSpeed; } set { characterRevealSpeed = value; } }

    public FaceEmotion Emotion { get { return emotion; } }
}
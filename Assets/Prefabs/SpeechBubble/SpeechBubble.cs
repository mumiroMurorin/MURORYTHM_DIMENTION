using UnityEngine;
using TMPro;
using UnityEngine.Serialization;

public abstract class SpeechBubble : MonoBehaviour
{
    [SerializeField] protected TextMeshProUGUI tmp;

    public void Speak(string text, SpeechBubbleConfig config)
    {
        OnSpeak(text, config);
    }

    protected abstract void OnSpeak(string text, SpeechBubbleConfig config);

    public void ShutUp()
    {
        if (this == null) { return; }
        if (this.gameObject == null) { return; }

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
    [FormerlySerializedAs("characterRevealSpeed")]
    [Tooltip("しゃべり終わるまでの秒数")]
    [SerializeField] float speechDuration = 1f;
    [Tooltip("感情")]
    [SerializeField] FaceEmotion emotion = FaceEmotion.Normal;

    public float FontSize { get { return fontSize; } set { fontSize = value; } }
    public Color FontColor { get { return fontColor; } set { fontColor = value; } }
    public float SpeechDuration { get { return speechDuration; } set { speechDuration = value; } }
    public FaceEmotion Emotion { get { return emotion; } }
}

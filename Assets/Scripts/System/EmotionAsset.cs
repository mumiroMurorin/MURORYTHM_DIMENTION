using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(fileName = "EmotionAsset", menuName = "ScriptableObject/EmotionAsset")]
public class EmotionAsset : ScriptableObject
{
    [SerializeField] EmotionToSprite[] emotionToSprites;

    public void ApplySprite(FaceEmotion emotion, Image image)
    {
        foreach(var e in emotionToSprites)
        {
            if (e.CheckCondition(emotion))
            {
                e.ApplySprite(image);
                break;
            }
        }
    }

    [System.Serializable]
    class EmotionToSprite
    {
        [SerializeField] FaceEmotion emotion;
        [SerializeField] Sprite sprite;

        public bool CheckCondition(FaceEmotion emotion)
        {
            return this.emotion == emotion;
        }

        public void ApplySprite(Image image)
        {
            if(image == null) { return; }
            image.sprite = this.sprite;
        }
    }
}

/// <summary>
/// シキボー(とかのキャラ)の感情
/// </summary>
public enum FaceEmotion
{
    Normal = 1,
    Smile1 = 10,
    Smile2 = 11,
    Angry = 20,
    Surprised = 30,
    Null = 40,
}

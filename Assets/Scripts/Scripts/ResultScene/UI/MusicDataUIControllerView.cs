using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Refactoring.UIInResultScene
{
    public class MusicDataUIControllerView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI musicTitle_text;
        [SerializeField] TextMeshProUGUI composer_text;
        [SerializeField] Image thumbnail_image;

        public void SetMusicData(MusicData musicData)
        {
            if (musicData == null) { return; }

            if (musicTitle_text && musicData.MusicName != null) { musicTitle_text.text = musicData.MusicName; }
            if (composer_text && musicData.ComposerName != null) { composer_text.text = musicData.ComposerName; }
            if (thumbnail_image && musicData.MusicSprite != null) { thumbnail_image.sprite = musicData.MusicSprite; }
        }
    }

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UIInResultScene
{
    public class MusicInfoView : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI musicTitle_text;
        [SerializeField] TextMeshProUGUI composer_text;
        [SerializeField] Image jacket_image;

        public void OnChangeMusicData(MusicData musicData)
        {
            if (musicData == null) { return; }

            if (musicTitle_text && musicData.MusicName != null) { musicTitle_text.text = musicData.MusicName; }
            if (composer_text && musicData.ComposerName != null) { composer_text.text = musicData.ComposerName; }
            if (jacket_image && musicData.MusicSprite != null) { jacket_image.sprite = musicData.MusicSprite; }
        }
    }

}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ReadyToPlayUIControllerView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI titleTmp;
    [SerializeField] TextMeshProUGUI composerTmp;
    [SerializeField] Image musicImage;
    [SerializeField] Image difficultyImage;


    public void SetMusicData(MusicData musicData)
    {
        if (musicData == null) { return; }

        if (titleTmp) { titleTmp.text = musicData.MusicName; }
        if (composerTmp) { composerTmp.text = musicData.ComposerName; }
        if (musicImage) { musicImage.sprite = musicData.MusicSprite; }
        if (difficultyImage) {  }
    }
}

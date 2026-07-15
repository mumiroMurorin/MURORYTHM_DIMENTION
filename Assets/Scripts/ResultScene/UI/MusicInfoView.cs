using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UIInResultScene
{
    public class MusicInfoView : MonoBehaviour
    {
        [SerializeField] SymphonyTypePresentationDatabase symphonyTypePresentationDatabase;
        [SerializeField] TextMeshProUGUI musicTitle_text;
        [SerializeField] TextMeshProUGUI composer_text;
        [SerializeField] Image jacket_image;
        [SerializeField] Image difficultyImage;

        SymphonyType symphonyType;
        Difficulty difficulty;
        bool isSetSymphonyType;
        bool isSetDifficulty;

        public void OnChangeMusicData(MusicData musicData)
        {
            if (musicData == null) { return; }

            if (musicTitle_text && musicData.MusicName != null) { musicTitle_text.text = musicData.MusicName; }
            if (composer_text && musicData.ComposerName != null) { composer_text.text = musicData.ComposerName; }
            if (jacket_image && musicData.MusicSprite != null) { jacket_image.sprite = musicData.MusicSprite; }

            symphonyType = musicData.SymphonyType;
            isSetSymphonyType = true;

            SetDifficultyImage(symphonyType, difficulty);
        }
        
        public void OnChangeDifficulty(Difficulty difficulty)
        {
            this.difficulty = difficulty;
            isSetDifficulty = true;

            SetDifficultyImage(symphonyType, difficulty);
        }

        private void SetDifficultyImage(SymphonyType symphonyType, Difficulty difficulty)
        {
            if (difficultyImage == null) { return; }

            Sprite sprite = symphonyTypePresentationDatabase?.GetResultDifficultySprite(symphonyType, difficulty);
            if (sprite == null)
            {
                Debug.LogWarning($"[MusicInfoView] Result difficulty sprite is not set: {symphonyType}, {difficulty}");
                return;
            }

            difficultyImage.sprite = sprite;
        }
    }
}

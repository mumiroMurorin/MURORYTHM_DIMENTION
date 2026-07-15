using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UIInResultScene
{
    public class DifficultyViewController : MonoBehaviour
    {
        [SerializeField] SymphonyTypePresentationDatabase symphonyTypePresentationDatabase;
        [SerializeField] Transform difficultyViewParent;

        MusicData musicData;
        Difficulty difficulty;
        bool isSetSymphonyType;
        bool isSetDifficulty;
        DifficultyView currentDifficultyView;

        public void OnChangeDifficulty(Difficulty difficulty)
        {
            this.difficulty = difficulty;
            isSetDifficulty = true;

            if (isSetSymphonyType && isSetDifficulty) { SetProperty(musicData, difficulty); }
        }

        public void OnChangeMusicData(MusicData data)
        {
            this.musicData = data;
            isSetSymphonyType = true;

            if (isSetSymphonyType && isSetDifficulty) { SetProperty(musicData, difficulty); }
        }

        private void SetProperty(MusicData data, Difficulty difficulty)
        {
            if (data == null) { return; }

            GenerateDifficultyViewIfNeeded(data.SymphonyType);
            if (currentDifficultyView == null) { return; }

            currentDifficultyView.OnChangeDifficulty(difficulty);
            currentDifficultyView.OnChangeLevel(data.GetDifficulty(difficulty));
        }

        private void GenerateDifficultyViewIfNeeded(SymphonyType symphonyType)
        {
            if (currentDifficultyView != null) { return; }

            DifficultyView difficultyViewPrefab = symphonyTypePresentationDatabase?.GetDifficultyViewPrefab(symphonyType);
            if (difficultyViewPrefab == null)
            {
                Debug.LogWarning($"[DifficultyViewController] DifficultyView prefab is not set: {symphonyType}");
                return;
            }

            Transform parent = difficultyViewParent != null ? difficultyViewParent : transform;
            currentDifficultyView = Instantiate(difficultyViewPrefab, parent);
            currentDifficultyView.transform.localPosition = Vector3.zero;
            currentDifficultyView.transform.localRotation = Quaternion.identity;
            currentDifficultyView.transform.localScale = Vector3.one;

            if (currentDifficultyView.transform is RectTransform rectTransform)
            {
                rectTransform.anchoredPosition = Vector2.zero;
            }
        }
    }

}

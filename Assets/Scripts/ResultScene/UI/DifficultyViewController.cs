using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UIInResultScene
{
    public class DifficultyViewController : MonoBehaviour
    {
        [SerializeField] SymphonyTypeToView[] views;

        MusicData musicData;
        Difficulty difficulty;
        bool isSetSymphonyType;
        bool isSetDifficulty;

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
            if (views == null) { return; }

            foreach(var view in views)
            {
                view.SetActive(view.CheckCondition(data.SymphonyType));
                if (view.CheckCondition(data.SymphonyType))
                {
                    view.DifficultyView.OnChangeDifficulty(difficulty);
                    view.DifficultyView.OnChangeLevel(data.GetDifficulty(difficulty));
                }
            }
        }

        [System.Serializable]
        class SymphonyTypeToView
        {
            [SerializeField] SymphonyType symphonyType;
            [SerializeField] DifficultyView difficultyView;

            public DifficultyView DifficultyView { get { return difficultyView; } }

            public bool CheckCondition(SymphonyType symphonyType)
            {
                return this.symphonyType == symphonyType;
            }

            public void SetActive(bool isActive)
            {
                difficultyView.gameObject.SetActive(isActive);
            }
        }
    }

}

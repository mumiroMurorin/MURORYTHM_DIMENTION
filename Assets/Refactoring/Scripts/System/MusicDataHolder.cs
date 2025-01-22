using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    public class MusicDataHolder : IMusicDataSetter, IMusicDataGetter, IChartDataSetter, IChartDataGetter
    {
        // �I���y��
        ReactiveProperty<MusicData> music = new ReactiveProperty<MusicData>();
        public IReadOnlyReactiveProperty<MusicData> Music { get { return music; } }
        public void SetMusicData(MusicData data)
        {
            music.Value = data;
        }

        // �I���Փx
        ReactiveProperty<DifficulityName> difficulty = new ReactiveProperty<DifficulityName>(DifficulityName.Initiate);
        public IReadOnlyReactiveProperty<DifficulityName> Difficulty { get { return difficulty; } }
        public void SetDifficulty(DifficulityName difficulty)
        {
            this.difficulty.Value = difficulty;
        }

        // ���ʃf�[�^
        ChartData chart;
        ChartData IChartDataGetter.Chart { get { return chart; } }
        void IChartDataSetter.SetChartData(ChartData data)
        {
            chart = data;
        }
    }
}


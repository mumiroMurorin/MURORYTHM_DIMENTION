using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    public class MusicDataHolder : IMusicDataSetter, IMusicDataGetter, IChartDataSetter, IChartDataGetter
    {
        // 選択楽曲
        ReactiveProperty<MusicData> music = new ReactiveProperty<MusicData>();
        public IReadOnlyReactiveProperty<MusicData> Music { get { return music; } }
        public void SetMusicData(MusicData data)
        {
            music.Value = data;
        }

        // 選択難易度
        ReactiveProperty<DifficulityName> difficulty = new ReactiveProperty<DifficulityName>(DifficulityName.Initiate);
        public IReadOnlyReactiveProperty<DifficulityName> Difficulty { get { return difficulty; } }
        public void SetDifficulty(DifficulityName difficulty)
        {
            this.difficulty.Value = difficulty;
        }

        // 譜面データ
        ChartData chart;
        ChartData IChartDataGetter.Chart { get { return chart; } }
        void IChartDataSetter.SetChartData(ChartData data)
        {
            chart = data;
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

public class MusicDataHolder : IMusicDataSetter, IMusicDataGetter
{
    ReactiveProperty<MusicData> music = new ReactiveProperty<MusicData>();
    public IReadOnlyReactiveProperty<MusicData> Music { get { return music; } }

    public void SetMusicData(MusicData data)
    {
        if (data != null)
        {
            MusicRecordPersistence.LoadAndApply(data);
        }

        music.Value = data;
    }

    ReactiveProperty<Difficulty> difficulty = new ReactiveProperty<Difficulty>(global::Difficulty.Easy);
    public IReadOnlyReactiveProperty<Difficulty> Difficulty { get { return difficulty; } }

    public void SetDifficulty(Difficulty difficulty)
    {
        int[] values = Enum.GetValues(typeof(Difficulty)).Cast<int>().ToArray();
        this.difficulty.Value = (Difficulty)Mathf.Clamp((int)difficulty, values.Min(), values.Max());
    }
}

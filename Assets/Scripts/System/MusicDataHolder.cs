using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using System.Linq;

public class MusicDataHolder : IMusicDataSetter, IMusicDataGetter
{
    // 選択楽曲
    ReactiveProperty<MusicData> music = new ReactiveProperty<MusicData>();
    public IReadOnlyReactiveProperty<MusicData> Music { get { return music; } }
    public void SetMusicData(MusicData data)
    {
        music.Value = data;
    }

    // 選択難易度
    ReactiveProperty<Difficulty> difficulty = new ReactiveProperty<Difficulty>(global::Difficulty.Easy);
    public IReadOnlyReactiveProperty<Difficulty> Difficulty { get { return difficulty; } }
    public void SetDifficulty(Difficulty difficulty)
    {
        // 列挙型の値を取得して int 配列に変換
        int[] values = Enum.GetValues(typeof(Difficulty)).Cast<int>().ToArray();

        this.difficulty.Value = (Difficulty)Mathf.Clamp((int)difficulty, values.Min(), values.Max());
    }
}

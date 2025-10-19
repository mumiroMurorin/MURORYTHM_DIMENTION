using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using System;
using System.Linq;

public class MusicDataListHolder : IMusicDataListGetter, IMusicDataListSetter
{
    // 楽曲リスト
    List<MusicData> musicDataListOrigin = new List<MusicData>();
    ReactiveCollection<MusicData> musicDataListSorted = new ReactiveCollection<MusicData>();
    IReadOnlyReactiveCollection<MusicData> IMusicDataListGetter.MusicDatasSorted => musicDataListSorted;
    void IMusicDataListSetter.SetMusicList(List<MusicData> musicDatas)
    {
        if (musicDatas == null) { return; }

        musicDataListOrigin.Clear();
        musicDataListSorted.Clear();

        // ディープコピー
        foreach (var data in musicDatas)
        {
            musicDataListOrigin.Add(data);
            musicDataListSorted.Add(data);
        }

        // 選択楽曲の更新
        currentMusicData.Value = musicDataListSorted[musicIndexSelected.Value];
    }

    MusicData IMusicDataListGetter.GetMusicData(int index)
    {
        if (index >= musicDataListSorted.Count) { return null; }
        if (index < 0) { return null; }

        return musicDataListSorted[index];
    }

    // 選択楽曲インデックス
    ReactiveProperty<int> musicIndexSelected = new ReactiveProperty<int>(0);
    IReadOnlyReactiveProperty<int> IMusicDataListGetter.CurrentMusicIndex => musicIndexSelected;
    void IMusicDataListSetter.SetMusicIndex(int value)
    {
        if (value < 0) { musicIndexSelected.Value = musicDataListSorted.Count - 1; }
        else { musicIndexSelected.Value = value % musicDataListSorted.Count; }

        currentMusicData.Value = musicDataListSorted[musicIndexSelected.Value];
    }

    // 選択楽曲
    ReactiveProperty<MusicData> currentMusicData = new ReactiveProperty<MusicData>();
    IReadOnlyReactiveProperty<MusicData> IMusicDataListGetter.CurrentMusicData => currentMusicData;


    // 選択難易度
    ReactiveProperty<Difficulty> difficulty = new ReactiveProperty<Difficulty>(Difficulty.Easy);
    IReadOnlyReactiveProperty<Difficulty> IMusicDataListGetter.Difficulty { get { return difficulty; } }
    void IMusicDataListSetter.SetDifficulty(Difficulty difficulty)
    {
        // 列挙型の値を取得して int 配列に変換
        int[] values = Enum.GetValues(typeof(Difficulty)).Cast<int>().ToArray();

        this.difficulty.Value = (Difficulty)Mathf.Clamp((int)difficulty, values.Min(), values.Max());
    }

}

public interface IMusicDataListGetter
{
    IReadOnlyReactiveCollection<MusicData> MusicDatasSorted { get; }

    MusicData GetMusicData(int index);

    IReadOnlyReactiveProperty<Difficulty> Difficulty { get; }

    IReadOnlyReactiveProperty<MusicData> CurrentMusicData { get; }

    IReadOnlyReactiveProperty<int> CurrentMusicIndex { get; }
}

public interface IMusicDataListSetter
{
    void SetMusicList(List<MusicData> musicDatas);

    void SetMusicIndex(int value);

    void SetDifficulty(Difficulty difficulty);
}
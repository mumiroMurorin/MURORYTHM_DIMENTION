using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UniRx;

public class SelectSceneDataHolder : ISelectSceneDataGetter, ISelectSceneDataSetter
{
    // 楽曲リスト
    List<MusicData> musicDataListOrigin = new List<MusicData>();
    ReactiveCollection<MusicData> musicDataListSorted = new ReactiveCollection<MusicData>();
    IReadOnlyReactiveCollection<MusicData> ISelectSceneDataGetter.MusicDatasSorted => musicDataListSorted;
    void ISelectSceneDataSetter.SetMusicList(List<MusicData> musicDatas)
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
    MusicData ISelectSceneDataGetter.GetMusicData(int index)
    {
        if (index >= musicDataListSorted.Count) { return null; }
        if (index < 0) { return null; }

        return musicDataListSorted[index];
    }


    // 選択楽曲インデックス
    ReactiveProperty<int> musicIndexSelected = new ReactiveProperty<int>(0);
    IReadOnlyReactiveProperty<int> ISelectSceneDataGetter.CurrentMusicIndex => musicIndexSelected;
    void ISelectSceneDataSetter.SetMusicIndex(int value)
    {
        if (value < 0) { musicIndexSelected.Value = musicDataListSorted.Count - 1; }
        else { musicIndexSelected.Value = value % musicDataListSorted.Count; }

        currentMusicData.Value = musicDataListSorted[musicIndexSelected.Value];
    }


    // オプションリスト
    List<OptionType> optionList = new List<OptionType>() { 
         OptionType.NoteSpeed,
         OptionType.Offset,
         OptionType.BGMVolume,
         OptionType.SEVolume,
    };
    void ISelectSceneDataSetter.SetOptionList(List<OptionType> optionTypes)
    {
        optionList = new List<OptionType>(optionTypes);
    }
    OptionType ISelectSceneDataGetter.GetOptionType(int index)
    {
        if (index >= optionList.Count) { return OptionType.None; }
        if (index < 0) { return OptionType.None; }

        return optionList[index];
    }


    // 選択オプションインデックス
    ReactiveProperty<int> optionIndexSelected = new ReactiveProperty<int>(0);
    IReadOnlyReactiveProperty<int> ISelectSceneDataGetter.CurrentOptionIndex => optionIndexSelected;
    void ISelectSceneDataSetter.SetOptionIndex(int value)
    {
        if (value < 0) { optionIndexSelected.Value = optionList.Count - 1; }
        else { optionIndexSelected.Value = value % optionList.Count; }
    }


    // 選択楽曲
    ReactiveProperty<MusicData> currentMusicData = new ReactiveProperty<MusicData>();
    IReadOnlyReactiveProperty<MusicData> ISelectSceneDataGetter.CurrentMusicData => currentMusicData;


    // 選択難易度
    ReactiveProperty<Difficulty> difficulty = new ReactiveProperty<Difficulty>(Difficulty.Initiate);
    IReadOnlyReactiveProperty<Difficulty> ISelectSceneDataGetter.Difficulty { get { return difficulty; } }
    void ISelectSceneDataSetter.SetDifficulty(Difficulty difficulty)
    {
        // 列挙型の値を取得して int 配列に変換
        int[] values = Enum.GetValues(typeof(Difficulty)).Cast<int>().ToArray();

        this.difficulty.Value = (Difficulty)Mathf.Clamp((int)difficulty, values.Min(), values.Max());
    }


}

public interface ISelectSceneDataGetter
{
    IReadOnlyReactiveCollection<MusicData> MusicDatasSorted { get; }

    IReadOnlyReactiveProperty<int> CurrentMusicIndex { get; }

    IReadOnlyReactiveProperty<int> CurrentOptionIndex { get; }

    IReadOnlyReactiveProperty<Difficulty> Difficulty { get; }

    IReadOnlyReactiveProperty<MusicData> CurrentMusicData { get; }

    MusicData GetMusicData(int index);

    OptionType GetOptionType(int index);
}

public interface ISelectSceneDataSetter
{
    void SetMusicIndex(int value);

    void SetOptionList(List<OptionType> optionTypes);

    void SetOptionIndex(int value);

    void SetDifficulty(Difficulty difficulty);

    void SetMusicList(List<MusicData> musicDatas);
}

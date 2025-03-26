using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UniRx;

namespace Refactoring
{
    public class MusicDataListHolder : ISelectSceneDataGetter, ISelectSceneDataSetter
    {
        List<MusicData> musicDataListOrigin = new List<MusicData>();
        ReactiveCollection<MusicData> musicDataListSorted = new ReactiveCollection<MusicData>();

        IReadOnlyReactiveCollection<MusicData> ISelectSceneDataGetter.MusicDatasSorted => musicDataListSorted;

        // 選択インデックス
        ReactiveProperty<int> musicIndexSelected = new ReactiveProperty<int>(0);
        IReadOnlyReactiveProperty<int> ISelectSceneDataGetter.CurrentSelectIndex => musicIndexSelected;
        void ISelectSceneDataSetter.SetSelectIndex(int value)
        {
            if(value < 0) { musicIndexSelected.Value = musicDataListSorted.Count - 1; }
            else { musicIndexSelected.Value = value % musicDataListSorted.Count; }
        }

        // 選択難易度
        ReactiveProperty<Difficulty> difficulty = new ReactiveProperty<Difficulty>(Difficulty.Initiate);
        IReadOnlyReactiveProperty<Difficulty> ISelectSceneDataGetter.Difficulty { get { return difficulty; } }

        void ISelectSceneDataSetter.SetDifficulty(Difficulty difficulty)
        {
            // 列挙型の値を取得して int 配列に変換
            int[] values = Enum.GetValues(typeof(Difficulty)).Cast<int>().ToArray();

            this.difficulty.Value = (Difficulty)Mathf.Clamp((int)difficulty, values.Min(), values.Max());
        }

        MusicData ISelectSceneDataGetter.GetMusicData(int index)
        {
            if(index >= musicDataListSorted.Count) { return null; }
            if(index < 0) { return null; }

            return musicDataListSorted[index];
        }

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
        }
    }

    public interface ISelectSceneDataGetter
    {
        IReadOnlyReactiveCollection<MusicData> MusicDatasSorted { get; }

        IReadOnlyReactiveProperty<int> CurrentSelectIndex { get; }

        IReadOnlyReactiveProperty<Difficulty> Difficulty { get; }

        MusicData GetMusicData(int index);
    }

    public interface ISelectSceneDataSetter
    {
        void SetSelectIndex(int value);

        void SetDifficulty(Difficulty difficulty);

        void SetMusicList(List<MusicData> musicDatas);
    }
}

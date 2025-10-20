using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UniRx;

public class SelectSceneDataHolder : ISelectSceneDataGetter, ISelectSceneDataSetter
{
    // オプションリスト
    List<OptionType> optionList = new List<OptionType>() { 
         OptionType.NoteSpeed,
         OptionType.Offset,
         OptionType.DivisionNum,
         OptionType.IsEnabledFastLate,
         OptionType.JudgementSEVolume,
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
}

public interface ISelectSceneDataGetter
{
    IReadOnlyReactiveProperty<int> CurrentOptionIndex { get; }

    OptionType GetOptionType(int index);
}

public interface ISelectSceneDataSetter
{
    void SetOptionList(List<OptionType> optionTypes);

    void SetOptionIndex(int value);
}

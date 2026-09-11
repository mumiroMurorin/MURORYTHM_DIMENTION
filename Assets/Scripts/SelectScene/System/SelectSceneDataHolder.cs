using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using UniRx;

public class SelectSceneDataHolder : ISelectSceneDataGetter, ISelectSceneDataSetter
{
    // �I�v�V�������X�g
    List<OptionType> optionList = new List<OptionType>() { 
         OptionType.NoteSpeed,
         OptionType.Offset,
         OptionType.NoteCurveRadius,
         OptionType.NoteVisibleDistance,
         OptionType.DivisionNum,
         OptionType.IsEnabledFastLate,
         OptionType.MainInfo,
         OptionType.SubInfo,
         OptionType.JudgementSEVolume,
    };

    void ISelectSceneDataSetter.SetOptionList(List<OptionType> optionTypes)
    {
        optionList = new List<OptionType>(optionTypes);
    }

    int ISelectSceneDataGetter.OptionCount => optionList.Count;

    OptionType ISelectSceneDataGetter.GetOptionType(int index)
    {
        if (index >= optionList.Count) { return OptionType.None; }
        if (index < 0) { return OptionType.None; }

        return optionList[index];
    }


    // �I���I�v�V�����C���f�b�N�X
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

    int OptionCount { get; }

    OptionType GetOptionType(int index);
}

public interface ISelectSceneDataSetter
{
    void SetOptionList(List<OptionType> optionTypes);

    void SetOptionIndex(int value);
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class OptionDataListController : MonoBehaviour
{
    ISelectSceneDataGetter selectSceneDataGetter;
    ISelectSceneDataSetter selectSceneDataSetter;
    IOptionSetter optionSetter;

    [Inject]
    public void Construct(ISelectSceneDataGetter selectSceneDataGetter, ISelectSceneDataSetter selectSceneDataSetter, IOptionSetter optionSetter)
    {
        this.selectSceneDataGetter = selectSceneDataGetter;
        this.selectSceneDataSetter = selectSceneDataSetter;
        this.optionSetter = optionSetter;
    }

    /// <summary>
    /// OptionTopic‚ÌˆÚ“®
    /// </summary>
    /// <param name="index"></param>
    public void MoveOptionTopic(int delta)
    {
        selectSceneDataSetter.SetOptionIndex(selectSceneDataGetter.CurrentOptionIndex.Value + delta);
    }

    public void ChangeTopicValue(int delta)
    {
        OptionType currentType = selectSceneDataGetter.GetOptionType(selectSceneDataGetter.CurrentOptionIndex.Value);
        optionSetter.SetOption(currentType, delta);
    }
}

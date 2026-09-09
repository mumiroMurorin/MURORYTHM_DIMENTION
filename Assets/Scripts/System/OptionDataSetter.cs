using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class OptionDataSetter : MonoBehaviour
{
    IOptionSetter optionSetter;
    IOptionGetter optionGetter;

    [Inject]
    public void Constructor(IOptionSetter optionSetter, IOptionGetter optionGetter)
    {
        this.optionSetter = optionSetter;
        this.optionGetter = optionGetter;
    }

    public void SetOption(OptionAsset asset)
    {
        optionSetter?.SetOption(asset);
    }

    public void ResetTutorialGuideCharacterType()
    {
        optionSetter?.ResetTutorialGuideCharacterType();
    }

    public void SetCurrentTutorialGuideCharacterType(TutorialGuideCharacterType tutorialGuideCharacterType)
    {
        optionSetter?.SetCurrentTutorialGuideCharacterType(tutorialGuideCharacterType);
    }

    public void SetFirstPlayGuideRequired(bool isRequired)
    {
        optionSetter?.SetFirstPlayGuideRequired(isRequired);
    }

    public bool IsFirstPlayGuideRequired()
    {
        return optionGetter?.IsFirstPlayGuideRequired.Value ?? false;
    }
}

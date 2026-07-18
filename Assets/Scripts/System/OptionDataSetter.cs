using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class OptionDataSetter : MonoBehaviour
{
    IOptionSetter optionSetter;

    [Inject]
    public void Constructor(IOptionSetter optionSetter)
    {
        this.optionSetter = optionSetter;
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
}

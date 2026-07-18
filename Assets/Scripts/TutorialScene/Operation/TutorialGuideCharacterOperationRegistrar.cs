using UnityEngine;

public class TutorialGuideCharacterOperationRegistrar : MonoBehaviour
{
    [SerializeField] OperationDictionary operationDictionary;
    [SerializeField] TutorialGuideCharacterSelector selector;
    [SerializeField] bool createDefaultSelectorWhenNull = true;

    void Awake()
    {
        RegisterOperation();
    }

    void RegisterOperation()
    {
        if (operationDictionary == null) { return; }

        operationDictionary.RegisterOperation(OperationTag.Tutorial_SelectCreationGuide, () =>
        {
            SelectAndConfirm(TutorialGuideCharacterType.Creation);
        });
        operationDictionary.RegisterOperation(OperationTag.Tutorial_SelectShikibooGuide, () =>
        {
            SelectAndConfirm(TutorialGuideCharacterType.Shikiboo);
        });
        operationDictionary.RegisterOperation(OperationTag.Tutorial_SelectDestructionGuide, () =>
        {
            SelectAndConfirm(TutorialGuideCharacterType.Destruction);
        });
    }

    void SelectAndConfirm(TutorialGuideCharacterType characterType)
    {
        if (selector == null && createDefaultSelectorWhenNull)
        {
            selector = FindObjectOfType<TutorialGuideCharacterSelector>();
        }

        if (selector == null && createDefaultSelectorWhenNull)
        {
            selector = TutorialGuideCharacterSelector.CreateDefault();
        }

        selector?.SelectAndConfirm(characterType);
    }
}

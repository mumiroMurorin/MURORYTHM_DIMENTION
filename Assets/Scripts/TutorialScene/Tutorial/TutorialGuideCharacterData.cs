using UnityEngine;

[CreateAssetMenu(fileName = "TutorialGuideCharacterData", menuName = "ScriptableObject/Tutorial/TutorialGuideCharacterData")]
public class TutorialGuideCharacterData : ScriptableObject
{
    [SerializeField] TutorialGuideCharacterType characterType = TutorialGuideCharacterType.Shikiboo;
    [SerializeField] EmotionAsset emotionAsset;
    [SerializeField] TutorialActionAsset tutorialActionAsset;

    public TutorialGuideCharacterType CharacterType => characterType;
    public EmotionAsset EmotionAsset => emotionAsset;
    public TutorialActionAsset TutorialActionAsset => tutorialActionAsset;
}

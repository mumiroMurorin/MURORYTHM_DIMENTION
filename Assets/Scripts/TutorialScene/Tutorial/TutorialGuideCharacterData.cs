using UnityEngine;

[CreateAssetMenu(fileName = "TutorialGuideCharacterData", menuName = "ScriptableObject/Tutorial/TutorialGuideCharacterData")]
public class TutorialGuideCharacterData : ScriptableObject
{
    [SerializeField] TutorialGuideCharacterType characterType = TutorialGuideCharacterType.Shikiboo;
    [SerializeField] EmotionAsset emotionAsset;
    [SerializeField] TutorialActionAsset tutorialActionAsset;
    [SerializeField] StageType stageType = StageType.CreationNoon;
    [SerializeField] SymphonyType symphonyType = SymphonyType.None;

    public TutorialGuideCharacterType CharacterType => characterType;
    public EmotionAsset EmotionAsset => emotionAsset;
    public TutorialActionAsset TutorialActionAsset => tutorialActionAsset;
    public StageType StageType => stageType;
    public SymphonyType SymphonyType => symphonyType;
}

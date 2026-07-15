using UIInResultScene;
using UnityEngine;

[CreateAssetMenu(fileName = "SymphonyTypePresentationData", menuName = "ScriptableObject/SymphonyTypePresentationData")]
public class SymphonyTypePresentationData : ScriptableObject
{
    [Header("Symphony Type")]
    [SerializeField] SymphonyType symphonyType = SymphonyType.None;

    [Header("Text")]
    [SerializeField] string masterDifficultyText;

    [Header("Dynamic Prefabs")]
    [SerializeField] GameObject circlePrefab;
    [SerializeField] MusicTopic musicTopicPrefab;
    [SerializeField] DifficultyView difficultyViewPrefab;
    [SerializeField] InteractNoteEffectSpawnController interactNoteEffectControllerPrefab;
    [SerializeField] UIInGameOverScene.CharacterAnimationController characterAnimationControllerPrefab;

    [Header("Ready View")]
    [SerializeField] DifficultyToSprite[] readyDifficultySprites;

    [Header("Result View")]
    [SerializeField] DifficultyToSprite[] resultDifficultySprites;
    [SerializeField] Animator resultAdditionalAnimatorPrefab;

    public SymphonyType SymphonyType => symphonyType;
    public string MasterDifficultyText => masterDifficultyText;
    public GameObject CirclePrefab => circlePrefab;
    public MusicTopic MusicTopicPrefab => musicTopicPrefab;
    public DifficultyView DifficultyViewPrefab => difficultyViewPrefab;
    public InteractNoteEffectSpawnController InteractNoteEffectControllerPrefab => interactNoteEffectControllerPrefab;
    public UIInGameOverScene.CharacterAnimationController CharacterAnimationControllerPrefab => characterAnimationControllerPrefab;
    public Animator ResultAdditionalAnimatorPrefab => resultAdditionalAnimatorPrefab;

    public Sprite GetReadyDifficultySprite(Difficulty difficulty)
    {
        return GetSprite(readyDifficultySprites, difficulty);
    }

    public Sprite GetResultDifficultySprite(Difficulty difficulty)
    {
        return GetSprite(resultDifficultySprites, difficulty);
    }

    private static Sprite GetSprite(DifficultyToSprite[] sprites, Difficulty difficulty)
    {
        if (sprites == null) { return null; }

        foreach (DifficultyToSprite sprite in sprites)
        {
            if (sprite != null && sprite.CheckCondition(difficulty))
            {
                return sprite.Sprite;
            }
        }

        return null;
    }
}

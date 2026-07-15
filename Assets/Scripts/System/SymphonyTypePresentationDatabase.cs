using System;
using UIInResultScene;
using UnityEngine;

[CreateAssetMenu(fileName = "SymphonyTypePresentationDatabase", menuName = "ScriptableObject/SymphonyTypePresentationDatabase")]
public class SymphonyTypePresentationDatabase : ScriptableObject
{
    [SerializeField] SymphonyTypePresentationData[] presentationDatas;
    [SerializeField] SymphonyTypePresentationData fallbackPresentationData;

    public SymphonyTypePresentationData[] PresentationDatas => presentationDatas;
    public SymphonyTypePresentationData FallbackPresentationData => fallbackPresentationData;

    public SymphonyTypePresentationData Get(SymphonyType symphonyType)
    {
        SymphonyTypePresentationData data = Find(symphonyType);
        if (data != null) { return data; }

        SymphonyTypePresentationData fallback = GetFallbackData();
        if (fallback != null)
        {
            Debug.LogWarning($"[SymphonyTypePresentationDatabase] {symphonyType} data was not found. Use fallback.");
            return fallback;
        }

        Debug.LogWarning($"[SymphonyTypePresentationDatabase] {symphonyType} data was not found.");
        return null;
    }

    public bool TryGet(SymphonyType symphonyType, out SymphonyTypePresentationData data)
    {
        data = Get(symphonyType);
        return data != null;
    }

    public string GetMasterDifficultyText(SymphonyType symphonyType)
    {
        return GetValueWithFallback(symphonyType, data => data.MasterDifficultyText, nameof(SymphonyTypePresentationData.MasterDifficultyText));
    }

    public GameObject GetCirclePrefab(SymphonyType symphonyType)
    {
        return GetValueWithFallback(symphonyType, data => data.CirclePrefab, nameof(SymphonyTypePresentationData.CirclePrefab));
    }

    public MusicTopic GetMusicTopicPrefab(SymphonyType symphonyType)
    {
        return GetValueWithFallback(symphonyType, data => data.MusicTopicPrefab, nameof(SymphonyTypePresentationData.MusicTopicPrefab));
    }

    public DifficultyView GetDifficultyViewPrefab(SymphonyType symphonyType)
    {
        return GetValueWithFallback(symphonyType, data => data.DifficultyViewPrefab, nameof(SymphonyTypePresentationData.DifficultyViewPrefab));
    }

    public InteractNoteEffectSpawnController GetInteractNoteEffectControllerPrefab(SymphonyType symphonyType)
    {
        return GetValueWithFallback(symphonyType, data => data.InteractNoteEffectControllerPrefab, nameof(SymphonyTypePresentationData.InteractNoteEffectControllerPrefab));
    }

    public UIInGameOverScene.CharacterAnimationController GetCharacterAnimationControllerPrefab(SymphonyType symphonyType)
    {
        return GetValueWithFallback(symphonyType, data => data.CharacterAnimationControllerPrefab, nameof(SymphonyTypePresentationData.CharacterAnimationControllerPrefab));
    }

    public Animator GetResultAdditionalAnimatorPrefab(SymphonyType symphonyType)
    {
        return GetValueWithFallback(symphonyType, data => data.ResultAdditionalAnimatorPrefab, nameof(SymphonyTypePresentationData.ResultAdditionalAnimatorPrefab));
    }

    public Sprite GetReadyDifficultySprite(SymphonyType symphonyType, Difficulty difficulty)
    {
        return GetValueWithFallback(symphonyType, data => data.GetReadyDifficultySprite(difficulty), nameof(SymphonyTypePresentationData.GetReadyDifficultySprite));
    }

    public Sprite GetResultDifficultySprite(SymphonyType symphonyType, Difficulty difficulty)
    {
        return GetValueWithFallback(symphonyType, data => data.GetResultDifficultySprite(difficulty), nameof(SymphonyTypePresentationData.GetResultDifficultySprite));
    }

    private T GetValueWithFallback<T>(SymphonyType symphonyType, Func<SymphonyTypePresentationData, T> selector, string valueName) where T : class
    {
        SymphonyTypePresentationData data = Find(symphonyType);
        T value = data != null ? selector(data) : null;
        if (value != null) { return value; }

        SymphonyTypePresentationData fallback = GetFallbackData();
        if (fallback == null || ReferenceEquals(data, fallback)) { return null; }

        T fallbackValue = selector(fallback);
        if (fallbackValue != null)
        {
            Debug.LogWarning($"[SymphonyTypePresentationDatabase] {symphonyType} {valueName} is not set. Use fallback.");
            return fallbackValue;
        }

        return null;
    }

    private SymphonyTypePresentationData GetFallbackData()
    {
        if (fallbackPresentationData != null) { return fallbackPresentationData; }

        // Compatibility path for older assets. New assets should set fallbackPresentationData explicitly.
        return Find(SymphonyType.None);
    }

    private SymphonyTypePresentationData Find(SymphonyType symphonyType)
    {
        if (presentationDatas == null) { return null; }

        SymphonyTypePresentationData found = null;
        foreach (SymphonyTypePresentationData data in presentationDatas)
        {
            if (data == null || data.SymphonyType != symphonyType) { continue; }

            if (found != null)
            {
                Debug.LogWarning($"[SymphonyTypePresentationDatabase] {symphonyType} data is duplicated.");
                continue;
            }

            found = data;
        }

        return found;
    }
}

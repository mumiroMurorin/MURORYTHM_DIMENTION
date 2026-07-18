using UnityEngine;

[CreateAssetMenu(fileName = "TutorialGuideCharacterDatabase", menuName = "ScriptableObject/Tutorial/TutorialGuideCharacterDatabase")]
public class TutorialGuideCharacterDatabase : ScriptableObject
{
    [SerializeField] TutorialGuideCharacterData[] characterDatas;
    [SerializeField] TutorialGuideCharacterData fallbackCharacterData;

    public TutorialGuideCharacterData Get(TutorialGuideCharacterType characterType)
    {
        TutorialGuideCharacterData data = Find(characterType);
        if (data != null) { return data; }

        if (fallbackCharacterData != null)
        {
            Debug.LogWarning($"[TutorialGuideCharacterDatabase] {characterType} data was not found. Use fallback.");
            return fallbackCharacterData;
        }

        TutorialGuideCharacterData shikiboo = Find(TutorialGuideCharacterType.Shikiboo);
        if (shikiboo != null)
        {
            Debug.LogWarning($"[TutorialGuideCharacterDatabase] {characterType} data was not found. Use Shikiboo fallback.");
            return shikiboo;
        }

        Debug.LogWarning($"[TutorialGuideCharacterDatabase] {characterType} data was not found.");
        return null;
    }

    TutorialGuideCharacterData Find(TutorialGuideCharacterType characterType)
    {
        if (characterDatas == null) { return null; }

        TutorialGuideCharacterData found = null;
        foreach (TutorialGuideCharacterData data in characterDatas)
        {
            if (data == null || data.CharacterType != characterType) { continue; }

            if (found != null)
            {
                Debug.LogWarning($"[TutorialGuideCharacterDatabase] {characterType} data is duplicated.");
                continue;
            }

            found = data;
        }

        return found;
    }
}

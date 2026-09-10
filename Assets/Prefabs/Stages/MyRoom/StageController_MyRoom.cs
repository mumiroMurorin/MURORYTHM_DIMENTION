using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class StageController_MyRoom : MonoBehaviour, IStageController
{
    [SerializeField] SymphonyTypePresentationDatabase symphonyTypePresentationDatabase;

    [Header("タイトルテキスト設定")]
    [SerializeField] WorldLoopingClippedTMPText titleText;

    [Header("難易度テキスト設定")]
    [SerializeField] TextMeshPro difficultyText;

    [Header("レベルテキスト設定")]
    [SerializeField] TextMeshPro levelText;

    [Header("テキスト描画順設定")]
    [SerializeField] bool overrideTextRenderQueue;
    [SerializeField] int titleTextRenderQueue = 3000;
    [SerializeField] int difficultyTextRenderQueue = 3000;
    [SerializeField] int levelTextRenderQueue = 3000;

    [Header("ジャケット設定")]
    [SerializeField] SpriteRenderer jacketSpriteRenderer;
    [SerializeField] Image jacketImage;

    void IStageController.Initialize(IMusicDataGetter musicDataGetter)
    {
        if (musicDataGetter == null || musicDataGetter.Music == null || musicDataGetter.Music.Value == null)
        {
            Debug.LogWarning("[StageControllerDestructionJirai] MusicDataGetter is not set.");
            return;
        }

        MusicData musicData = musicDataGetter.Music.Value;
        Difficulty difficulty = musicDataGetter.Difficulty != null ? musicDataGetter.Difficulty.Value : Difficulty.Normal;

        // シーン上に配置済みの表示コンポーネントへ、曲データだけを流し込む。
        SetTitleText(musicData.MusicName);
        SetDifficultyText(GetDifficultyText(musicData, difficulty));
        SetLevelText(GetLevelText(musicData, difficulty));
        ApplyTextRenderQueue();

        // ジャケットはSpriteRenderer / UI Imageのどちらでも受けられるようにする。
        SetJacket(musicData.MusicSprite);
    }

    void SetTitleText(string text)
    {
        if (titleText == null)
        {
            Debug.LogWarning("[StageControllerDestructionJirai] Title text controller is not set.");
            return;
        }

        titleText.SetText(text);
    }

    void SetDifficultyText(string text)
    {
        if (difficultyText == null)
        {
            Debug.LogWarning("[StageControllerDestructionJirai] Difficulty TextMeshPro is not set.");
            return;
        }

        difficultyText.text = text;
    }

    void SetLevelText(string text)
    {
        if (levelText == null)
        {
            Debug.LogWarning("[StageControllerDestructionJirai] Level TextMeshPro is not set.");
            return;
        }

        levelText.text = text;
    }

    void ApplyTextRenderQueue()
    {
        if (!overrideTextRenderQueue) { return; }

        // タイトルはループ用コピーが内部生成されるため、専用Controller側からまとめて反映する。
        if (titleText != null)
        {
            titleText.SetRenderQueue(titleTextRenderQueue);
        }

        ApplyRenderQueue(difficultyText, difficultyTextRenderQueue);
        ApplyRenderQueue(levelText, levelTextRenderQueue);
    }

    void ApplyRenderQueue(TextMeshPro target, int renderQueue)
    {
        if (target == null)
        {
            return;
        }

        Material material = target.fontMaterial;
        if (material == null)
        {
            return;
        }

        material.renderQueue = renderQueue;
        target.UpdateMeshPadding();
    }

    void SetJacket(Sprite sprite)
    {
        if (jacketSpriteRenderer != null)
        {
            jacketSpriteRenderer.sprite = sprite;
        }

        if (jacketImage != null)
        {
            jacketImage.sprite = sprite;
        }
    }

    string GetDifficultyText(MusicData musicData, Difficulty difficulty)
    {
        if (difficulty != Difficulty.Master)
        {
            return difficulty.ToString().ToUpper();
        }

        SymphonyType symphonyType = musicData != null ? musicData.SymphonyType : SymphonyType.None;
        string masterDifficultyText = symphonyTypePresentationDatabase?.GetMasterDifficultyText(symphonyType);
        if (!string.IsNullOrEmpty(masterDifficultyText))
        {
            return masterDifficultyText;
        }

        Debug.LogWarning($"[StageControllerDestructionJirai] Master difficulty text is not set: {symphonyType}");
        return Difficulty.Master.ToString().ToUpper();
    }

    string GetLevelText(MusicData musicData, Difficulty difficulty)
    {
        if (musicData == null)
        {
            return string.Empty;
        }

        int level = musicData.GetDifficulty(difficulty);
        return level >= 0 ? level.ToString() : string.Empty;
    }

}

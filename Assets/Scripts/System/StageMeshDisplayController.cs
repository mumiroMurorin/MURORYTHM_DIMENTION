using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public sealed class StageMeshDisplayController : StageController
{
    [SerializeField] CharacterSpawner characterSpawner;

    [Header("Title Mesh")]
    [SerializeField] FlyingTextSettings titleSettings;
    [SerializeField] Transform titleParent;
    [SerializeField] string titleComposerSeparator = " / ";
    [Min(1)] [SerializeField] int titleVisibleCharacterCount = 12;
    [SerializeField] OutlineSettings titleOutline;

    [Header("Difficulty Mesh")]
    [SerializeField] FlyingTextSettings difficultySettings;
    [SerializeField] Transform difficultyParent;
    [Min(1)] [SerializeField] int difficultyVisibleCharacterCount = 12;
    [SerializeField] OutlineSettings difficultyOutline;

    [Header("Outline Colors By Difficulty")]
    [FormerlySerializedAs("difficultyOutlineColors")]
    [SerializeField] DifficultyToColor[] outlineColors;

    [Header("Loop Animation")]
    [Tooltip("Value added to each character mesh width.")]
    [Min(0f)] [SerializeField] float characterSpacing;
    [Min(0f)] [SerializeField] float loopInterval = 0.4f;
    [Min(0.01f)] [SerializeField] float transitionDuration = 0.18f;
    [Min(0f)] [SerializeField] float transitionYOffset = 2f;

    [Header("Jacket Images")]
    [SerializeField] Image[] jacketImages;

    protected override void InitializeStage(MusicData musicData, Difficulty difficulty)
    {
        Color? outlineColor = GetOutlineColor(difficulty);

        SetLoopingText(
            titleParent,
            GetTitleText(musicData, titleComposerSeparator) + " ",
            titleSettings,
            titleVisibleCharacterCount,
            titleOutline,
            outlineColor);

        string difficultyAndLevel = GetDifficultyLevelText(
            GetDifficultyText(musicData, difficulty),
            GetLevelText(musicData, difficulty)) + " ";
        SetLoopingText(
            difficultyParent,
            difficultyAndLevel,
            difficultySettings,
            difficultyVisibleCharacterCount,
            difficultyOutline,
            outlineColor);

        SetImages(jacketImages, musicData.MusicSprite);
    }

    void SetLoopingText(
        Transform parent,
        string text,
        FlyingTextSettings settings,
        int visibleCount,
        OutlineSettings outline,
        Color? outlineColor)
    {
        if (parent == null || characterSpawner == null)
        {
            Debug.LogWarning($"[{nameof(StageMeshDisplayController)}] Text parent or CharacterSpawner is not set.", this);
            return;
        }

        LoopingFlyingText3D view = parent.GetComponent<LoopingFlyingText3D>();
        if (view == null)
        {
            view = parent.gameObject.AddComponent<LoopingFlyingText3D>();
        }

        view.Configure(
            characterSpawner,
            settings,
            visibleCount,
            characterSpacing,
            loopInterval,
            transitionDuration,
            transitionYOffset,
            outline,
            outlineColor);
        view.SetText(text);
    }

    Color? GetOutlineColor(Difficulty difficulty)
    {
        if (outlineColors == null) { return null; }

        foreach (DifficultyToColor item in outlineColors)
        {
            if (item != null && item.CheckCondition(difficulty))
            {
                return item.Color;
            }
        }

        return null;
    }
}

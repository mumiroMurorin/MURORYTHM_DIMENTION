using UnityEngine;
using UnityEngine.UI;

public sealed class StageMeshDisplayController : StageController
{
    [SerializeField] CharacterSpawner characterSpawner;

    [Header("Title Mesh")]
    [SerializeField] FlyingTextSettings titleSettings;
    [SerializeField] Transform titleParent;
    [SerializeField] string titleComposerSeparator = " / ";
    [Min(1)] [SerializeField] int titleVisibleCharacterCount = 12;

    [Header("Difficulty Mesh")]
    [SerializeField] FlyingTextSettings difficultySettings;
    [SerializeField] Transform difficultyParent;
    [Min(1)] [SerializeField] int difficultyVisibleCharacterCount = 12;

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
        SetLoopingText(
            titleParent,
            GetTitleText(musicData, titleComposerSeparator) + " ",
            titleSettings,
            titleVisibleCharacterCount);

        string difficultyAndLevel = GetDifficultyLevelText(
            GetDifficultyText(musicData, difficulty),
            GetLevelText(musicData, difficulty)) + " ";
        SetLoopingText(
            difficultyParent,
            difficultyAndLevel,
            difficultySettings,
            difficultyVisibleCharacterCount);

        SetImages(jacketImages, musicData.MusicSprite);
    }

    void SetLoopingText(
        Transform parent,
        string text,
        FlyingTextSettings settings,
        int visibleCount)
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
            transitionYOffset);
        view.SetText(text);
    }
}

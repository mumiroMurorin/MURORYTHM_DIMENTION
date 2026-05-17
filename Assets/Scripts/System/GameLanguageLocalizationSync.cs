using System.Collections;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class GameLanguageLocalizationSync : SingletonMonoBehaviour<GameLanguageLocalizationSync>
{
    [SerializeField] private string japaneseLocaleCode = "ja";
    [SerializeField] private string englishLocaleCode = "en";

    private bool isLocalizationReady;
    private GameLanguage pendingLanguage = GameLanguage.Japanese;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        _ = Instance;
    }

    protected override void Awake()
    {
        base.Awake();

        LobbySceneDataHolder.SelectedLanguageChanged += OnSelectedLanguageChanged;
    }

    private IEnumerator Start()
    {
        yield return LocalizationSettings.InitializationOperation;

        isLocalizationReady = true;
        ApplyPendingLanguage();
    }

    private void OnDestroy()
    {
        LobbySceneDataHolder.SelectedLanguageChanged -= OnSelectedLanguageChanged;
    }

    private void OnSelectedLanguageChanged(GameLanguage language)
    {
        pendingLanguage = language;
        ApplyPendingLanguage();
    }

    private void ApplyPendingLanguage()
    {
        if (!isLocalizationReady)
        {
            return;
        }

        Locale locale = ResolveLocale(pendingLanguage);
        if (locale == null)
        {
            Debug.LogWarning($"GameLanguageLocalizationSync: locale not found for {pendingLanguage}.");
            return;
        }

        if (LocalizationSettings.SelectedLocale == locale)
        {
            return;
        }

        LocalizationSettings.SelectedLocale = locale;
    }

    private Locale ResolveLocale(GameLanguage language)
    {
        string localeCode = language switch
        {
            GameLanguage.English => englishLocaleCode,
            _ => japaneseLocaleCode,
        };

        if (LocalizationSettings.AvailableLocales == null)
        {
            return null;
        }

        return LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(localeCode));
    }
}

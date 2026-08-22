using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace UIInSelectScene
{
    public class OptionTopicViewBase : MonoBehaviour
    {
        [SerializeField] TableReference optionTextTable = "Option";
        [SerializeField] string optionNameKey;
        [SerializeField] string optionDetailKey;
        [SerializeField] TextMeshProUGUI optionNameText;
        [SerializeField] TextMeshProUGUI optionDetailText;

        string optionNameFallback;
        string optionDetailFallback;
        bool hasCachedFallback;

        protected virtual void OnEnable()
        {
            LocalizationSettings.SelectedLocaleChanged += OnSelectedLocaleChanged;
            ApplyLocalizedTexts();
        }

        protected virtual void OnDisable()
        {
            LocalizationSettings.SelectedLocaleChanged -= OnSelectedLocaleChanged;
        }

        private void OnSelectedLocaleChanged(Locale locale)
        {
            ApplyLocalizedTexts();
        }

        private void ApplyLocalizedTexts()
        {
            ResolveTextReferences();
            CacheFallbackTexts();

            ApplyText(optionNameText, optionNameKey, optionNameFallback);
            ApplyText(optionDetailText, optionDetailKey, optionDetailFallback);
        }

        private void ResolveTextReferences()
        {
            if (optionNameText != null && optionDetailText != null) { return; }

            foreach (var text in GetComponentsInChildren<TextMeshProUGUI>(true))
            {
                if (optionNameText == null && text.name == "OptionName")
                {
                    optionNameText = text;
                    continue;
                }

                if (optionDetailText == null && text.name == "OptionDetail")
                {
                    optionDetailText = text;
                }
            }
        }

        private void CacheFallbackTexts()
        {
            if (hasCachedFallback) { return; }

            optionNameFallback = optionNameText != null ? optionNameText.text : string.Empty;
            optionDetailFallback = optionDetailText != null ? optionDetailText.text : string.Empty;
            hasCachedFallback = true;
        }

        private void ApplyText(TMP_Text text, string key, string fallback)
        {
            if (text == null) { return; }

            text.text = ResolveText(key, fallback);
        }

        private string ResolveText(string key, string fallback)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrEmpty(optionTextTable.TableCollectionName))
            {
                return fallback;
            }

            var localizedText = LocalizationSettings.StringDatabase.GetLocalizedString(optionTextTable, key);
            return string.IsNullOrEmpty(localizedText) ? fallback : localizedText;
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace TransitionerInLobbyScene
{
    public class Transitioner_LoadData : IPhaseTransitionerInLobbyScene
    {
        [SerializeField] SerializeInterface<IPhaseTransitionableInLobbyScene> phaseTransitionable;
        [SerializeField] OperationDictionary operationDictionary;
        [SerializeField] LobbySceneDataController dataController;

        readonly PhaseStatusInLobbyScene status = PhaseStatusInLobbyScene.LoadData;

        bool IPhaseTransitionerInLobbyScene.ConditionChecker(PhaseStatusInLobbyScene status)
        {
            return this.status == status;
        }

        void IPhaseTransitionerInLobbyScene.Transition()
        {
            Debug.Log("【Transition】Transition to \"LoadData\"");

            SoundManager.Instance.PlayBGM(BGM_Type.Lobby);

            RegisterOperation();

            TransitionNextPhase();
        }

        /// <summary>
        /// 次のフェーズへの移動
        /// </summary>
        private void TransitionNextPhase()
        {
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInLobbyScene.FadeIn);
        }

        private void RegisterOperation()
        {
            operationDictionary.RegisterOperation(OperationTag.Lobby_PlayTutorial, () => { TransitionFadeOutPhase(true); });
            operationDictionary.RegisterOperation(OperationTag.Lobby_SkipTutorial, () => { TransitionFadeOutPhase(false); });
            operationDictionary.RegisterOperation(OperationTag.Lobby_SelectJapanese, () => { SetLanguage(GameLanguage.Japanese); });
            operationDictionary.RegisterOperation(OperationTag.Lobby_SelectEnglish, () => { SetLanguage(GameLanguage.English); });
        }

        private void TransitionFadeOutPhase(bool isPlayTutorial)
        {
            dataController?.DataSetter?.SetPlayTutorial(isPlayTutorial);
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInLobbyScene.FadeOut);
        }

        private void SetLanguage(GameLanguage language)
        {
            dataController?.DataSetter?.SetSelectedLanguage(language);
            ApplyLocalization(language);
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInLobbyScene.ConfirmTutorial);
        }

        private void ApplyLocalization(GameLanguage language)
        {
            if (LocalizationSettings.AvailableLocales == null)
            {
                Debug.LogWarning("【Transitioner_LoadData】Localization available locales is not initialized.");
                return;
            }

            var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(GetLocaleCode(language)));
            if (locale == null)
            {
                Debug.LogWarning($"【Transitioner_LoadData】Locale was not found for {language}.");
                return;
            }

            LocalizationSettings.SelectedLocale = locale;
        }

        private string GetLocaleCode(GameLanguage language)
        {
            return language == GameLanguage.English ? "en" : "ja";
        }
    }
}

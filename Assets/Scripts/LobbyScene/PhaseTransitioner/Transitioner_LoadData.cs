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
            Debug.Log("ÅyTransitionÅzTransition to \"LoadData\"");

            SoundManager.Instance.PlayBGM(BGM_Type.Lobby);

            RegisterOperation();

            TransitionNextPhase();
        }

        /// <summary>
        /// éüÇÃÉtÉFÅ[ÉYÇ÷ÇÃà⁄ìÆ
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
            operationDictionary.RegisterOperation(OperationTag.Lobby_CautionPlaying1_Confirm, () => { phaseTransitionable?.Value.TransitionPhase(PhaseStatusInLobbyScene.CautionPlaying2); });
            operationDictionary.RegisterOperation(OperationTag.Lobby_CautionPlaying2_Confirm, () => { phaseTransitionable?.Value.TransitionPhase(PhaseStatusInLobbyScene.ConfirmTutorial); });
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
            phaseTransitionable?.Value.TransitionPhase(PhaseStatusInLobbyScene.CautionPlaying1);
        }

        private void ApplyLocalization(GameLanguage language)
        {
            if (LocalizationSettings.AvailableLocales == null)
            {
                Debug.LogWarning("ÅyTransitioner_LoadDataÅzLocalization available locales is not initialized.");
                return;
            }

            var locale = LocalizationSettings.AvailableLocales.GetLocale(new LocaleIdentifier(GetLocaleCode(language)));
            if (locale == null)
            {
                Debug.LogWarning($"ÅyTransitioner_LoadDataÅzLocale was not found for {language}.");
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

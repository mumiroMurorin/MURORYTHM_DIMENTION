using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;
using TransitionerInSelectScene;

public class SoundEventSubscriberInLobbyScene : MonoBehaviour
{
    [SerializeField] OperationDictionary operationDictionary;

    void Start()
    {
        Bind();
    }

    private void Bind()
    {
        operationDictionary?.RegisterOperation(OperationTag.Lobby_SelectJapanese, () => { SoundManager.Instance.PlaySE(SE_Type.AnyDecision); });
        operationDictionary?.RegisterOperation(OperationTag.Lobby_SelectEnglish, () => { SoundManager.Instance.PlaySE(SE_Type.AnyDecision); });
        operationDictionary?.RegisterOperation(OperationTag.Lobby_PlayTutorial, () => { SoundManager.Instance.PlaySE(SE_Type.AnyDecision); });
        operationDictionary?.RegisterOperation(OperationTag.Lobby_SkipTutorial, () => { SoundManager.Instance.PlaySE(SE_Type.AnyCancel); });
    }
}


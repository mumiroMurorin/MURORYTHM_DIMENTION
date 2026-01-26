using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;
using TransitionerInSelectScene;

public class SoundEventSubscriberInGameOverScene : MonoBehaviour
{
    [SerializeField] OperationDictionary operationDictionary;

    void Start()
    {
        Bind();
    }

    private void Bind()
    {
        operationDictionary?.RegisterOperation(OperationTag.GameOver_Continue, () => { SoundManager.Instance.PlaySE(SE_Type.ContinueGame); });
        operationDictionary?.RegisterOperation(OperationTag.GameOver_FinishGame, () => { SoundManager.Instance.PlaySE(SE_Type.FinishGame); });
    }
}


using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class OperationDictionary : MonoBehaviour
{
    Dictionary<OperationTag, Action> operationTagToAction = new Dictionary<OperationTag, Action>();

    public void RegisterOperation(OperationTag tag, Action action)
    {
        if (!operationTagToAction.TryAdd(tag, action))
        {
            operationTagToAction[tag] += action;
        }
    }

    public Action GetOperation(OperationTag tag)
    {
        return operationTagToAction.GetValueOrDefault(tag);
    }
}

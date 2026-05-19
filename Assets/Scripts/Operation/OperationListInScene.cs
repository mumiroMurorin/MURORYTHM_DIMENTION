using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization.Tables;

[CreateAssetMenu(fileName = "OperationListInScene", menuName = "ScriptableObject/OperationListInScene")]
public class OperationListInScene : ScriptableObject
{
    [SerializeField] OperationInPhase[] assetsList;
    [SerializeField] TableReference textTableReference;

    public IEnumerable<OperationInPhase> AssetsList { get { return assetsList; } }
    public TableReference TextTableReference => textTableReference;
}

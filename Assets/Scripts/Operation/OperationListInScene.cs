using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "OperationListInScene", menuName = "ScriptableObject/OperationListInScene")]
public class OperationListInScene : ScriptableObject
{
    [SerializeField] OperationInPhase[] assetsList;

    public IEnumerable<OperationInPhase> AssetsList { get { return assetsList; } }
}

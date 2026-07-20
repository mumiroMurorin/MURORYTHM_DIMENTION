using Tutorial;
using UnityEngine;
using UnityEngine.Localization.Tables;
[CreateAssetMenu(fileName = "TutorialActionAsset", menuName = "ScriptableObject/Tutorial/TutorialActionAsset")]
public class TutorialActionAsset : ScriptableObject
{
    [SerializeField] TableReference textTableReference;
    [SerializeReference, SubclassSelector] TutorialActionNode[] actions;

    public TableReference TextTableReference => textTableReference;
    public TutorialActionNode[] Actions => actions;
}

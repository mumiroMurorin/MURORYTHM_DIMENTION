using Tutorial;
using UnityEngine;

[CreateAssetMenu(fileName = "TutorialActionAsset", menuName = "ScriptableObject/Tutorial/TutorialActionAsset")]
public class TutorialActionAsset : ScriptableObject
{
    [SerializeReference, SubclassSelector] TutorialActionNode[] actions;

    public TutorialActionNode[] Actions => actions;
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class RhythmConfigurableBarCollider : MonoBehaviour, IRhythmConfigurableBarCollider
    {
        [SerializeField] DeployableLineObject bar;

        public EditMode EditMode => EditMode.EditBarConfig;

        DeployableLineObject IRhythmConfigurableBarCollider.barObj => bar;
    }

    public interface IRhythmConfigurableBarCollider : IInteractableCollider
    {
        DeployableLineObject barObj { get; }
    }

}

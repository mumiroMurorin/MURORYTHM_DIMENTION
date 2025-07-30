using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class RhythmConfigurableSubDivisionCollider : MonoBehaviour, IRhythmConfigurableSubDivisionCollider
    {
        [SerializeField] DeployableLineObject subDivision;

        public EditMode EditMode => EditMode.EditSubDivisionConfig;

        DeployableLineObject IRhythmConfigurableSubDivisionCollider.subdivisionObj => subDivision;
    }

    public interface IRhythmConfigurableSubDivisionCollider : IInteractableCollider
    {
        DeployableLineObject subdivisionObj { get; }
    }
}

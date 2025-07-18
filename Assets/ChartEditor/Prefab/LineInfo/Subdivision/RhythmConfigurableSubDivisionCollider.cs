using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class RhythmConfigurableSubDivisionCollider : MonoBehaviour, IRhythmConfigurableSubDivisionCollider
    {
        [SerializeField] SerializeInterface<ISubdivisionLineData> subDivision;

        public EditMode EditMode => EditMode.EditSubDivisionConfig;

        ISubDivisionDataGetter IRhythmConfigurableSubDivisionCollider.SubDivisionDataGetter => subDivision.Value.SubDivisionData;
    }

    public interface IRhythmConfigurableSubDivisionCollider : IInteractableCollider
    {
        ISubDivisionDataGetter SubDivisionDataGetter { get; }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class RhythmConfigurableSubDivisionCollider : MonoBehaviour, IRhythmConfigurableSubDivisionCollider
    {
        [SerializeField] SerializeInterface<ISubDivisionDataGetter> subDivision;

        ISubDivisionDataGetter IRhythmConfigurableSubDivisionCollider.SubDivisionDataGetter => subDivision.Value;
    }

    public interface IRhythmConfigurableSubDivisionCollider
    {
        ISubDivisionDataGetter SubDivisionDataGetter { get; }
    }
}

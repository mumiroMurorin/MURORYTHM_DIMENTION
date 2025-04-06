using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class RhythmConfigurableCollider : MonoBehaviour, IRhythmConfigurableCollider
    {
        [SerializeField] SerializeInterface<IBarDataGetter> bar;

        IBarDataGetter IRhythmConfigurableCollider.BarDataGetter => bar.Value;
    }

}

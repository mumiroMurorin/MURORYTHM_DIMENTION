using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class RhythmConfigurableBarCollider : MonoBehaviour, IRhythmConfigurableBarCollider
    {
        [SerializeField] SerializeInterface<IBarDataGetter> bar;

        IBarDataGetter IRhythmConfigurableBarCollider.BarDataGetter => bar.Value;
    }

    public interface IRhythmConfigurableBarCollider
    {
        IBarDataGetter BarDataGetter { get; }
    }

}

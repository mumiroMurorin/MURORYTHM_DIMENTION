using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class RhythmConfigurableBarCollider : MonoBehaviour, IRhythmConfigurableBarCollider
    {
        [SerializeField] SerializeInterface<IBarLineData> bar;

        public EditMode EditMode => EditMode.EditBarConfig;

        IBarDataGetter IRhythmConfigurableBarCollider.BarDataGetter => bar.Value.BarData;
    }

    public interface IRhythmConfigurableBarCollider : IInteractableCollider
    {
        IBarDataGetter BarDataGetter { get; }
    }

}

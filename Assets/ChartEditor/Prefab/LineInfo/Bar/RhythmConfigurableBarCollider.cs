using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChartEditor
{
    public class RhythmConfigurableBarCollider : MonoBehaviour, IRhythmConfigurableBarCollider
    {
        [SerializeField] SerializeInterface<IBarDataGetter> bar;

        public EditMode EditMode => EditMode.EditBarConfig;

        IBarDataGetter IRhythmConfigurableBarCollider.BarDataGetter => bar.Value;
    }

    public interface IRhythmConfigurableBarCollider : IInteractableCollider
    {
        IBarDataGetter BarDataGetter { get; }
    }

}

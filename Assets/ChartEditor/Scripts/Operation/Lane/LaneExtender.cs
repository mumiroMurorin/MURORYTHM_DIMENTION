using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;
using static UndoRedo.History;

namespace ChartEditor
{
    public class LaneExtender : MonoBehaviour
    {
        IChartEditorDataSetter dataSetter;

        [Inject]
        public void Construct(IChartEditorDataSetter dataSetter)
        {
            this.dataSetter = dataSetter;
        }

        public void ChangeChartLength(int delta)
        {
            Record(() => {
                dataSetter.ChangeChartLength(delta);
            },
            () => {
                dataSetter.ChangeChartLength(-delta);
            });
        }
    }

}
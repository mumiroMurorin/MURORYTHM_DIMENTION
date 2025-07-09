using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

namespace ChartEditor
{
    public class BarDataController : MonoBehaviour
    {
        IChartEditorDataGetter dataGetter;
        IChartEditorDataSetter dataSetter;
        INotesDataGetter notesGetter;

        [Inject]
        public void Constructor(IChartEditorDataGetter dataGetter, INotesDataGetter notesGetter, IChartEditorDataSetter dataSetter)
        {
            this.dataGetter = dataGetter;
            this.notesGetter = notesGetter;
            this.dataSetter = dataSetter;
        }

        private void Start()
        {
            
        }


        public void AddBar()
        {

        }

        public void RemoveBar()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        private void SetBarData()
        {

        }

    }

}
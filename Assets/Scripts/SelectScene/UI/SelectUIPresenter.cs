using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;


namespace Refactoring.UIInSelectScene
{
    public class SelectUIPresenter : MonoBehaviour
    {
        IMusicDataGetter musicData_model;

        [Inject] 
        public void Construct(IMusicDataGetter musicDataGetter)
        {
            musicData_model = musicDataGetter;
        }

        private void Start()
        {
            Bind();
            SetEvent();
        }

        private void Bind()
        {
            
        }

        private void SetEvent()
        {

        }
    }
}

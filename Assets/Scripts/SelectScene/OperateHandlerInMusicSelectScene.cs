using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

namespace Refactoring
{
    public class OperateHandlerInMusicSelectScene : MonoBehaviour
    {
        IMusicDataSetter musicDataSetter;

        [Inject]
        public void Construct(IMusicDataSetter musicDataSetter)
        {
            this.musicDataSetter = musicDataSetter;
        }

        
    }

    public interface IOperateHandlerInMusicSelectScene
    {
        void 
    }

}
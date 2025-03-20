using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    public class MusicDataListHolder : MonoBehaviour//, ISelectSceneDataGetter, ISelectSceneDataSetter
    {
        [SerializeField] List<MusicData> musicDataList;

        List<MusicData> musicDataListSorted;
        ReactiveProperty<int> currentSelectIndex = new ReactiveProperty<int>(0);

        //IReadOnlyReactiveProperty<int> ISelectSceneDataGetter.CurrentSelectIndex { get { return currentSelectIndex; } }

        //void ISelectSceneDataSetter.SetSelectIndex(int value)
        //{
        //    currentSelectIndex.Value = value;
        //}

        private void Start()
        {
            musicDataListSorted.AddRange(musicDataList); 
        }
        
    }

    public interface ISelectSceneDataGetter
    {
        IReadOnlyReactiveProperty<int> CurrentSelectIndex { get; }
    }

    public interface ISelectSceneDataSetter
    {
        void SetSelectIndex(int value);
    }
}

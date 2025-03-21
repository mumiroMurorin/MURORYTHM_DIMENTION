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

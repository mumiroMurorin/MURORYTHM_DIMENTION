using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Refactoring
{
    public class MusicDataListHolder : MonoBehaviour
    {
        [SerializeField] List<MusicData> musicDataList;

        List<MusicData> musicDataListSorted;
        int currentSelectIndex = 0;

        private void Start()
        {
            musicDataListSorted.AddRange(musicDataList); 
        }



    }

}

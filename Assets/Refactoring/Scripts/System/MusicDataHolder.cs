using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    public class MusicDataHolder : IMusicDataSetter, IMusicDataGetter
    {
        // ‘I‘ðŠy‹È
        ReactiveProperty<MusicData> musicSelected = new ReactiveProperty<MusicData>();
        public IReadOnlyReactiveProperty<MusicData> MusicSelected { get { return musicSelected; } }
        public void SetMusicData(MusicData musicData)
        {
            musicSelected.Value = musicData;
        }

    }
}


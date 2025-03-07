using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    public class OptionHolder : INoteSpawnDataOptionHolder, IVolumeGetter, IVolumeSetter
    {
        /// <summary>
        /// ノーツが1秒間に動く(unity単位)速度
        /// </summary>
        public float NoteSpeed { get; set; } = 80f;

        // SE関係
        ReactiveProperty<float> seVolume = new ReactiveProperty<float>(0.8f);
        IReadOnlyReactiveProperty<float> IVolumeGetter.SEVolume => seVolume;
        void IVolumeSetter.SetSEVolume(float value)
        {
            seVolume.Value = value;
        }

        // BGM関係
        ReactiveProperty<float> bgmVolume = new ReactiveProperty<float>(0.8f);
        IReadOnlyReactiveProperty<float> IVolumeGetter.BGMVolume => bgmVolume;
        void IVolumeSetter.SetBGMVolume(float value)
        {
            bgmVolume.Value = value;
        }
    }

    public interface INoteSpawnDataOptionHolder
    {
        public float NoteSpeed { get; }
    }

    public interface IVolumeGetter
    {
        IReadOnlyReactiveProperty<float> SEVolume { get; }

        IReadOnlyReactiveProperty<float> BGMVolume { get; }
    }

    public interface IVolumeSetter
    {
        void SetSEVolume(float value);

        void SetBGMVolume(float value);
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

namespace Refactoring
{
    public class OptionHolder : INoteSpawnDataOptionHolder, IVolumeGetter, IVolumeSetter
    {
        /// <summary>
        /// �m�[�c��1�b�Ԃɓ���(unity�P��)���x
        /// </summary>
        public float NoteSpeed { get; set; } = 200f;

        // SE�֌W
        ReactiveProperty<float> seVolume = new ReactiveProperty<float>(0.8f);
        IReadOnlyReactiveProperty<float> IVolumeGetter.SEVolume => seVolume;
        void IVolumeSetter.SetSEVolume(float value)
        {
            seVolume.Value = value;
        }

        // BGM�֌W
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

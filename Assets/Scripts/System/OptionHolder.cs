using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

public class OptionHolder : INoteSpawnDataOptionHolder, IVolumeGetter, IVolumeSetter, IOptionGetter
{
    /// <summary>
    /// ÉmÅ[ÉcÇ™1ïbä‘Ç…ìÆÇ≠(unityíPà )ë¨ìx
    /// </summary>
    ReactiveProperty<float> noteSpeed = new ReactiveProperty<float>(1f);
    public IReadOnlyReactiveProperty<float> NoteSpeed => noteSpeed;
    public void SetNoteSpeed(float speed)
    {
        noteSpeed.Value = speed;
    }

    // SEä÷åW
    ReactiveProperty<float> seVolume = new ReactiveProperty<float>(0.8f);
    public IReadOnlyReactiveProperty<float> SEVolume => seVolume;
    void IVolumeSetter.SetSEVolume(float value)
    {
        seVolume.Value = value;
    }

    // BGMä÷åW
    ReactiveProperty<float> bgmVolume = new ReactiveProperty<float>(0.8f);
    public IReadOnlyReactiveProperty<float> BGMVolume => bgmVolume;
    void IVolumeSetter.SetBGMVolume(float value)
    {
        bgmVolume.Value = value;
    }

    // ÉIÉtÉZÉbÉgä÷åW
    ReactiveProperty<float> offset = new ReactiveProperty<float>(0);
    public IReadOnlyReactiveProperty<float> OffsetMs => offset;
    void SetOffset(float value)
    {
        offset.Value = value;
    }
}

public interface INoteSpawnDataOptionHolder
{
    IReadOnlyReactiveProperty<float> NoteSpeed { get; }

    IReadOnlyReactiveProperty<float> OffsetMs { get; }
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

public interface IOptionGetter
{
    IReadOnlyReactiveProperty<float> NoteSpeed { get; }

    IReadOnlyReactiveProperty<float> OffsetMs { get; }

    IReadOnlyReactiveProperty<float> SEVolume { get; }

    IReadOnlyReactiveProperty<float> BGMVolume { get; }
}

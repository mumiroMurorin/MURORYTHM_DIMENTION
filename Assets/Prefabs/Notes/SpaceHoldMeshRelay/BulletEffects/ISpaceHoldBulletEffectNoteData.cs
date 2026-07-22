using UnityEngine;

public interface ISpaceHoldBulletEffectNoteData
{
    int HoldNumber { get; }

    bool IsSpaceHoldEnd { get; }

    Vector2[] Vertices { get; }
}

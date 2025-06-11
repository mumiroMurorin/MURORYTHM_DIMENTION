using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicNoteAnimation : MonoBehaviour
{
    [SerializeField] Renderer targetRenderer;
    [SerializeField] string bufferName = "_MainTex";
    [Header("1ƒtƒŒ[ƒ€ŠÔ‚Ì“®‚«•")]
    [SerializeField] Vector2 speed;

    Vector2 currentOffset;

    private void Start()
    {
        if (targetRenderer == null) { return; }

        currentOffset = targetRenderer.material.GetTextureOffset(bufferName);
    }

    private void FixedUpdate()
    {
        if(targetRenderer == null) { return; }

        currentOffset = new Vector2((currentOffset.x + speed.x) % 1f, (currentOffset.y + speed.y) % 1f);
        targetRenderer.material.SetTextureOffset(bufferName, currentOffset);
    }
}

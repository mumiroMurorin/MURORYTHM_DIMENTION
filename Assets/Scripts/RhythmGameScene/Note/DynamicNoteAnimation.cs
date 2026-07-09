using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DynamicNoteAnimation : MonoBehaviour
{
    [SerializeField] Renderer targetRenderer;
    [SerializeField] string bufferName = "_MainTex";
    [Header("1フレーム間の動き幅")]
    [SerializeField] Vector2 speed;

    Vector2 currentOffset;

    private void Start()
    {
        if (targetRenderer == null) { return; }

        currentOffset = targetRenderer.material.GetTextureOffset(bufferName);
    }

    private void Update()
    {
        if (targetRenderer == null) { return; }

        // 以前の「FixedUpdate 1回あたりの移動量」を保ったまま、描画フレームに合わせて滑らかに進める
        float fixedDeltaTime = Mathf.Max(Time.fixedDeltaTime, Mathf.Epsilon);
        Vector2 frameSpeed = speed * (Time.deltaTime / fixedDeltaTime);

        currentOffset = new Vector2(
            Mathf.Repeat(currentOffset.x + frameSpeed.x, 1f),
            Mathf.Repeat(currentOffset.y + frameSpeed.y, 1f));
        targetRenderer.material.SetTextureOffset(bufferName, currentOffset);
    }
}

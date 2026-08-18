using UnityEngine;

public class DynamicNoteAnimation : MonoBehaviour
{
    [SerializeField] Renderer targetRenderer;
    [SerializeField] string bufferName = "_MainTex";
    [Header("1秒間のUV移動量")]
    [SerializeField] Vector2 speed;

    Material targetMaterial;
    Vector2 currentOffset;

    private void Start()
    {
        if (targetRenderer == null) { return; }

        targetMaterial = targetRenderer.material;
        currentOffset = targetMaterial.GetTextureOffset(bufferName);
    }

    private void Update()
    {
        if (targetMaterial == null) { return; }

        Vector2 frameOffset = speed * Time.deltaTime;

        currentOffset = new Vector2(
            Mathf.Repeat(currentOffset.x + frameOffset.x, 1f),
            Mathf.Repeat(currentOffset.y + frameOffset.y, 1f));
        targetMaterial.SetTextureOffset(bufferName, currentOffset);
    }
}

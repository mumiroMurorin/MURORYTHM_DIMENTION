using UnityEngine;

public class DynamicNoteAnimation : MonoBehaviour
{
    [SerializeField] Renderer targetRenderer;
    [SerializeField] string bufferName = "_MainTex";
    [Header("1秒間のUV移動量")]
    [SerializeField] Vector2 speed;

    Material targetMaterial;
    ITimeGetter timer;
    Vector2 initialOffset;

    public void Initialize(ITimeGetter timer)
    {
        this.timer = timer;
    }

    private void Start()
    {
        if (targetRenderer == null) { return; }

        targetMaterial = targetRenderer.material;
        initialOffset = targetMaterial.GetTextureOffset(bufferName);
    }

    private void Update()
    {
        if (targetMaterial == null) { return; }
        if (timer == null) { return; }

        Vector2 offset = initialOffset + speed * timer.Time;

        targetMaterial.SetTextureOffset(
            bufferName,
            new Vector2(
                Mathf.Repeat(offset.x, 1f),
                Mathf.Repeat(offset.y, 1f)));
    }
}

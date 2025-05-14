using UnityEngine;

public class MaterialAnimation : MonoBehaviour
{
    [SerializeField] Material material; 
    [SerializeField] int rows = 3; 
    [SerializeField] int columns = 3; 
    [SerializeField] float frameRate = 30f;
    [SerializeField] bool isReverse;

    int currentFrame;
    float frameTime;
    Vector2 textureScale;

    void Start()
    {
        if (material == null)
        {
            material = GetComponent<Renderer>().material;
        }

        // スプライトシートのUVスケールを設定
        textureScale = new Vector2(1f / columns, 1f / rows);
        material.SetTextureScale("_MainTex", textureScale);

        frameTime = 1f / frameRate;
    }

    void Update()
    {
        frameTime -= Time.deltaTime;
        if (frameTime <= 0f)
        {
            frameTime = 1f / frameRate;

            // フレームを進める
            if (!isReverse) { currentFrame = (currentFrame + 1) % (rows * columns); }
            else { currentFrame = (currentFrame + (rows * columns - 2)) % (rows * columns); }

            // UVオフセットを計算して適用
            int row = currentFrame / columns;
            int column = currentFrame % columns;
            Vector2 offset = new Vector2(column * textureScale.x, 1f - textureScale.y - row * textureScale.y);
            material.SetTextureOffset("_MainTex", offset);
        }
    }
}

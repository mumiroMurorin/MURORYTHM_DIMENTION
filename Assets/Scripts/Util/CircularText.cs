using UnityEngine;
using TMPro;

[ExecuteInEditMode]
[RequireComponent(typeof(TextMeshProUGUI))]
public class CircularText : MonoBehaviour
{
    public float radius = 100f; // 円の半径
    public float angleRange = 180f; // 文字を配置する角度の範囲
    public float startAngle = 0f; // 文字の配置開始角度
    public bool clockwise = true; // 時計回り配置

    private TextMeshProUGUI textMeshPro;

    void Awake()
    {
        textMeshPro = GetComponent<TextMeshProUGUI>();
        UpdateText();
    }

    void OnValidate()
    {
        if (textMeshPro == null) textMeshPro = GetComponent<TextMeshProUGUI>();
        UpdateText();
    }

    public void UpdateText()
    {
        if (textMeshPro == null) return;

        textMeshPro.ForceMeshUpdate();
        TMP_TextInfo textInfo = textMeshPro.textInfo;

        int characterCount = textInfo.characterCount;
        if (characterCount == 0) return;

        // **文字ごとの幅を考慮**
        float totalWidth = 0f;
        float[] charWidths = new float[characterCount];

        for (int i = 0; i < characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            float charWidth = (charInfo.topRight.x - charInfo.bottomLeft.x);
            charWidths[i] = charWidth;
            totalWidth += charWidth;
        }

        // **最初の文字の開始位置を調整**
        float currentAngle = startAngle; // 変更点: 開始位置をそのまま使う

        for (int i = 0; i < characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            // **文字ごとの角度計算（均等ではなく、文字幅に応じて配置）**
            float charAngle = (charWidths[i] / totalWidth) * angleRange;
            float angle = currentAngle;
            float radian = angle * Mathf.Deg2Rad;

            Vector3 charPosition = new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0) * radius;
            Quaternion rotation = Quaternion.Euler(0, 0, angle + 90);

            // **文字の頂点を移動＆回転**
            for (int j = 0; j < 4; j++)
            {
                int vertexIndex = charInfo.vertexIndex + j;
                textInfo.meshInfo[charInfo.materialReferenceIndex].vertices[vertexIndex] += charPosition;
                textInfo.meshInfo[charInfo.materialReferenceIndex].vertices[vertexIndex] =
                    rotation * (textInfo.meshInfo[charInfo.materialReferenceIndex].vertices[vertexIndex] - charPosition) + charPosition;
            }

            // **次の文字の位置に進む**
            currentAngle += charAngle * (clockwise ? 1 : -1); // 変更点: 開始位置を基準に一方向へ
        }

        textMeshPro.UpdateVertexData();
    }
}

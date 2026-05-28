using UnityEngine;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(TextMeshProUGUI))]
public class CircularText : MonoBehaviour
{
    [Header("円形配置の設定")]
    [Tooltip("円の半径")]
    [SerializeField] float radius = 100f;
    [Tooltip("Radius をピンポンさせるかどうか")]
    [SerializeField] bool pingPongRadius = false;
    [Tooltip("Radius の変化量")]
    [SerializeField] float radiusPingPongDelta = 10f;
    [Tooltip("Radius が片道移動する時間")]
    [SerializeField] float radiusPingPongDuration = 1f;
    [Tooltip("配置する文字の角度の範囲（度）")]
    [SerializeField] float angleRange = 180f;
    [Tooltip("文字数に応じて角度を自動調整する場合はここにチェック")]
    [SerializeField] bool autoAdjustmentAngleRange = false;
    [Tooltip("1文字毎の角度")]
    [SerializeField] float angleOfCharacter;
    [Tooltip("円弧の中心となる角度（度）。この値を中心に文字が配置されます。")]
    [SerializeField] float centerAngle = 0f;
    [Tooltip("各文字に加える回転オフセット（度）。通常は90で、円の接線方向に沿わせます。")]
    [SerializeField] float characterRotationOffset = 90f;

    public float CenterAngle { set { centerAngle = value; } }

    public void SetAngleOfCharacter(float value)
    {
        angleOfCharacter = value;
    }

    public void SetAutoAdjustmentAngleRange(bool value)
    {
        autoAdjustmentAngleRange = value;
    }

    public void RefreshLayout()
    {
        if (tmp == null)
            tmp = GetComponent<TextMeshProUGUI>();
        if (tmp == null) return;

        tmp.ForceMeshUpdate();
        textInfo = tmp.textInfo;
        if (textInfo == null || textInfo.characterCount == 0)
            return;

        ApplyCircularLayout();
    }

    private TextMeshProUGUI tmp;
    private TMP_TextInfo textInfo;
    private string previousText;
    private float previousRadius;
    private float previousAngleRange;
    private bool previousAutoAdjustmentAngleRange;
    private float previousAngleOfCharacter;
    private float previousCenterAngle;
    private float previousCharacterRotationOffset;
    private bool previousPingPongRadius;
    private float previousRadiusPingPongDelta;
    private float previousRadiusPingPongDuration;
    private float previousAppliedRadius = float.MinValue;

    void Awake()
    {
        tmp = GetComponent<TextMeshProUGUI>();
        RefreshLayout();
        CacheLayoutState();
    }

    void Start()
    {
        tmp.ForceMeshUpdate();
        textInfo = tmp.textInfo;
        ApplyCircularLayout();

        previousText = tmp.text;
        CacheLayoutState();
    }

    void Update()
    {
        if (tmp == null)
            tmp = GetComponent<TextMeshProUGUI>();
        if (tmp == null)
            return;

        if (ShouldRefreshLayout())
        {
            RefreshLayout();
            CacheLayoutState();
        }
    }

    void OnValidate()
    {
        if (tmp == null) tmp = GetComponent<TextMeshProUGUI>();
        RefreshLayout();
    }

    void ApplyCircularLayout()
    {
        int totalCharacters = textInfo.characterCount;
        if (totalCharacters == 0)
            return;

        // 表示されている文字の数をカウント
        int visibleCount = 0;
        for (int i = 0; i < totalCharacters; i++)
        {
            if (textInfo.characterInfo[i].isVisible)
                visibleCount++;
        }
        if (visibleCount == 0)
            return;

        // 角度範囲の自動調整
        if (autoAdjustmentAngleRange)
        {
            angleRange = angleOfCharacter * visibleCount;
        }

        // 円弧の中心角(centerAngle)を基準に、全体の配置範囲(angleRange)が決まるので
        // 最初の文字の配置角度は centerAngle - (angleRange / 2)
        float startAngle = centerAngle - (angleRange / 2f);
        // 各文字の配置角度の間隔
        float angleStep = visibleCount > 1 ? angleRange / (visibleCount - 1) : 0f;
        int visibleIndex = 0;

        // 各文字について処理
        for (int i = 0; i < totalCharacters; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible)
                continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            // 文字の四角形の中央（ベースライン中心）を取得
            Vector3 charMidBaseline = (vertices[vertexIndex] + vertices[vertexIndex + 2]) / 2;
            // 各頂点を文字中心基準にシフト（相対座標にする）
            for (int j = 0; j < 4; j++)
            {
                vertices[vertexIndex + j] -= charMidBaseline;
            }

            // この文字の配置角度は、startAngle + visibleIndex * angleStep で決定
            float angle = startAngle + (visibleIndex * angleStep);
            float radian = angle * Mathf.Deg2Rad;
            // 円形上の配置位置を計算
            Vector3 targetPosition = new Vector3(Mathf.Cos(radian), Mathf.Sin(radian), 0) * GetCurrentRadius();
            // 文字の回転は、配置角度に characterRotationOffset を加えた角度で回転させる
            Quaternion rotation = Quaternion.Euler(0, 0, angle + characterRotationOffset);

            // 各頂点に回転と平行移動を適用
            for (int j = 0; j < 4; j++)
            {
                vertices[vertexIndex + j] = rotation * vertices[vertexIndex + j] + targetPosition;
            }

            visibleIndex++;
        }

        // 変更した頂点情報を各メッシュに反映
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
        }
    }

    float GetCurrentRadius()
    {
        if (!pingPongRadius || radiusPingPongDuration <= 0f || Mathf.Approximately(radiusPingPongDelta, 0f))
            return radius;

        float time = Application.isPlaying ? Time.unscaledTime : Time.realtimeSinceStartup;
        float cycle = Mathf.PingPong(time / radiusPingPongDuration, 1f);
        return radius + Mathf.Lerp(-radiusPingPongDelta, radiusPingPongDelta, cycle);
    }

    bool ShouldRefreshLayout()
    {
        if (previousText != tmp.text)
            return true;
        if (!Mathf.Approximately(previousRadius, radius))
            return true;
        if (!Mathf.Approximately(previousAngleRange, angleRange))
            return true;
        if (previousAutoAdjustmentAngleRange != autoAdjustmentAngleRange)
            return true;
        if (!Mathf.Approximately(previousAngleOfCharacter, angleOfCharacter))
            return true;
        if (!Mathf.Approximately(previousCenterAngle, centerAngle))
            return true;
        if (!Mathf.Approximately(previousCharacterRotationOffset, characterRotationOffset))
            return true;
        if (previousPingPongRadius != pingPongRadius)
            return true;
        if (!Mathf.Approximately(previousRadiusPingPongDelta, radiusPingPongDelta))
            return true;
        if (!Mathf.Approximately(previousRadiusPingPongDuration, radiusPingPongDuration))
            return true;

        float currentAppliedRadius = GetCurrentRadius();
        if (!Mathf.Approximately(previousAppliedRadius, currentAppliedRadius))
            return true;

        return false;
    }

    void CacheLayoutState()
    {
        previousText = tmp != null ? tmp.text : string.Empty;
        previousRadius = radius;
        previousAngleRange = angleRange;
        previousAutoAdjustmentAngleRange = autoAdjustmentAngleRange;
        previousAngleOfCharacter = angleOfCharacter;
        previousCenterAngle = centerAngle;
        previousCharacterRotationOffset = characterRotationOffset;
        previousPingPongRadius = pingPongRadius;
        previousRadiusPingPongDelta = radiusPingPongDelta;
        previousRadiusPingPongDuration = radiusPingPongDuration;
        previousAppliedRadius = GetCurrentRadius();
    }
}

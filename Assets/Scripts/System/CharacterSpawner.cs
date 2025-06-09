using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterSpawner : MonoBehaviour
{
    [SerializeField] FlyingTextSettings defaultSettings;
    [SerializeField] bool isZaxisReversal = true;

    private void Start()
    {
        defaultSettings.ApplySettings();
    }

    /// <summary>
    /// 文字オブジェクトのスポーン
    /// </summary>
    /// <param name="pos"></param>
    public GameObject SpawnCharacter(string str, FlyingTextSettings settings = default)
    {
        // 設定の反映
        if(settings != null) { settings.ApplySettings(); }

        var parent = new GameObject(str);
        var strObj = FlyingText.GetObject(str);

        strObj.transform.SetParent(parent.transform);
        strObj.transform.position = parent.transform.position;
        strObj.transform.rotation = isZaxisReversal ? parent.transform.rotation * Quaternion.Euler(0, 180f, 0f) : parent.transform.rotation;

        return parent;
    }
}

[System.Serializable]
public class FlyingTextSettings
{
    [SerializeField] Material defaultMaterial;
    public Material DefaultMaterial { get { return defaultMaterial; } set { defaultMaterial = value; } }

    [SerializeField] Material edgeMaterial;
    public Material EdgeMaterial { get { return edgeMaterial; } set { edgeMaterial = value; } }

    [SerializeField] int fontNumber;
    public int FontNumber { get { return fontNumber; } set { fontNumber = value; } }

    [SerializeField] float defaultSize;
    public float DefaultSize { get { return defaultSize; } set { defaultSize = value; } }

    [SerializeField] float defaultDepth;
    public float DefaultDepth { get { return defaultDepth; } set { defaultDepth = value; } }

    [SerializeField] TextAnchor textAnchor;
    public TextAnchor TextAnchor { get { return textAnchor; } set { textAnchor = value; } }

    public void ApplySettings()
    {
        FlyingText.defaultMaterial = this.defaultMaterial;
        FlyingText.defaultEdgeMaterial = this.edgeMaterial;
        FlyingText.defaultFont = this.fontNumber;
        FlyingText.defaultSize = this.defaultSize;
        FlyingText.defaultDepth = this.defaultDepth;
        FlyingText.anchor = this.textAnchor;
    }
}
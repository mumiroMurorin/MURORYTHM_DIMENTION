using System.Linq;
using NaughtyAttributes;
using UnityEngine;

[CreateAssetMenu(fileName = "OperationAssetUnit", menuName = "ScriptableObject/OperationAssetUnit")]
public class OperationAssetUnit : ScriptableObject
{
    [Label("Operation Tag")]
    [SerializeField] private OperationTag tag;

    [Label("Slider Range")]
    [MinValue(0), MaxValue(15)]
    [SerializeField] private int leftEdge;

    [MinValue(0), MaxValue(15)]
    [SerializeField] private int rightEdge;

    [Label("Theme Color")]
    [SerializeField] private Color themeColor = Color.red;

    [Label("Controller Rainbow")]
    [SerializeField] private bool controllerRainbow = false;

    [HideIf(nameof(controllerRainbow))]
    [Label("Controller Color")]
    [SerializeField] private Color controllerColor = Color.red;

    [Label("Text Key")]
    [SerializeField] private string textKey;

    public OperationTag Tag => tag;
    public int[] SliderIndices => Enumerable.Range(leftEdge, rightEdge - leftEdge + 1).ToArray();
    public Color ThemeColor => themeColor;
    public Color ControllerColor => controllerColor;
    public bool ControllerRainbow => controllerRainbow;
    public string TextKey => textKey;
}

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

    [Label("Controller Color")]
    [SerializeField] private Color controllerColor = Color.red;

    [Label("Text")]
    [SerializeField] private string text;

    public OperationTag Tag => tag;
    public int[] SliderIndices => Enumerable.Range(leftEdge, rightEdge - leftEdge + 1).ToArray();
    public Color ThemeColor => themeColor;
    public Color ControllerColor => controllerColor;
    public string Text => text;
}

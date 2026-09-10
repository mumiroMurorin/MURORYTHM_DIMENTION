using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;
using Deform;

public class GroundOptionSetter : MonoBehaviour
{
    [SerializeField] GameObject[] divisionLines;
    [SerializeField] BendDeformer bendDeformer;

    IOptionGetter optionGetter;

    [Inject]
    public void Construct(IOptionGetter optionGetter)
    {
        this.optionGetter = optionGetter;
    }

    private void Start()
    {
        Bind();
    }

    private void Bind()
    {
        optionGetter?.GroundDivisionNum
            .Subscribe(SetDivisionLines)
            .AddTo(this.gameObject);

        optionGetter?.NoteCurveRadius
            .Subscribe(SetBendAngle)
            .AddTo(this.gameObject);
    }

    private void SetDivisionLines(int divNum)
    {
        if(divisionLines.Length != 17) { return; }

        for (int i = 0; i < divisionLines.Length; i++) 
        {
            if (i == 0 || i == 16) 
            {
                divisionLines[i].SetActive(true);
            }
            else if(i % (16 / divNum) == 0)
            {
                divisionLines[i].SetActive(true);
            }
            else
            {
                divisionLines[i].SetActive(false);
            }
        }
    }

    private void SetBendAngle(float radius)
    {
        if (bendDeformer == null) { return; }

        float bendLength = Mathf.Abs(bendDeformer.Top - bendDeformer.Bottom);
        float factor = Mathf.Abs(bendDeformer.Factor);
        if (bendLength <= Mathf.Epsilon || factor <= Mathf.Epsilon) { return; }

        float direction = bendDeformer.Angle > 0f ? 1f : -1f;
        bendDeformer.Angle = direction
            * bendLength
            / Mathf.Max(radius, 0.01f)
            * Mathf.Rad2Deg
            / factor;
    }
}

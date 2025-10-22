using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UniRx;
using VContainer;

public class GroundOptionSetter : MonoBehaviour
{
    [SerializeField] GameObject[] divisionLines;

    [Inject] IOptionGetter optionGetter;

    private void Start()
    {
        Bind();
    }

    private void Bind()
    {
        optionGetter?.GroundDivisionNum
            .Subscribe(SetDivisionLines)
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
}

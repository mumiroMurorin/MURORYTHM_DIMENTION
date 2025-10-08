using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class GroundOptionSetter : MonoBehaviour
{
    [SerializeField] GameObject[] divisionLines;

    [Inject] IOptionGetter optionGetter;

    private void Start()
    {
        SetDivisionLines();
    }

    private void SetDivisionLines()
    {
        if(divisionLines.Length != 17) { return; }

        for (int i = 0; i < divisionLines.Length; i++) 
        {
            if (i == 0 || i == 16) 
            {
                divisionLines[i].SetActive(true);
            }
            else if(i % (16 / optionGetter.GroundDivisionNum.Value) == 0)
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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

namespace Refactoring
{
    public class Combo_View : MonoBehaviour
    {
        [Header("���R���{����\�����邩")]
        [SerializeField] int comboThreshold = 5;
        [SerializeField] TextMeshPro textMeshPro;
        [SerializeField] Animator animator;

        public void OnChangeCombo(int comboNum)
        {
            textMeshPro.text = comboNum.ToString();

            if(comboThreshold > comboNum)
            {
                textMeshPro.gameObject.SetActive(false);
            }
            else
            {
                textMeshPro.gameObject.SetActive(true);
                animator.SetTrigger("combo");
            }
        }
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

public class KeyBeamGroupController : MonoBehaviour
{
    [SerializeField] KeyBeamController[] keyBeams;

    ISliderInputGetter inputGetter;

    [Inject]
    public void Inject(ISliderInputGetter getter)
    {
        inputGetter = getter;
    }

    private void Start()
    {
        Bind();
    }

    private void Bind()
    {
        for (int i = 0; i < keyBeams.Length; i++)
        {
            // UniRx‚ÌŽd—l‚©‰½‚©’m‚ç‚È‚¢‚ªintŒ^‚ªŽQÆ“n‚µ‚³‚ê‚Ä‚µ‚Ü‚¤‚Ì‚Å
            // i‚ð’¼Ú‘ã“ü‚·‚é‚Ì‚Å‚Í‚È‚­index‚ð‰î‚·
            int index = i;

            inputGetter?.GetSliderInputReactiveProperty(index)
                .Subscribe(keyBeams[index].SetActive)
                .AddTo(this.gameObject);
        }
    }

}


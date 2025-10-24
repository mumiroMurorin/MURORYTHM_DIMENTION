using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;
using UniRx;

public class ChartLoaderInSelectScene : MonoBehaviour
{
    [Inject] IJudgementRecorder judgementRecorder;
    [Inject] IOptionGetter optionGetter;

    [SerializeField] ScoreSetterInSelectScene scoreSetter;
    [SerializeField] SerializeInterface<ITimeController> timeController;
    [SerializeField] SerializeInterface<IChartGenerator> chartGenerator;

    void Start()
    {
        Bind();
    }

    private void Bind()
    {
        // ノートスピードが変わった際はリセット
        optionGetter?.NoteSpeed
            .Subscribe(_ =>OnChangeNoteSpeed())
            .AddTo(this.gameObject);
    }

    private void OnChangeNoteSpeed()
    {
        scoreSetter?.Initialize();
        timeController?.Value?.ResetTimer();
        chartGenerator?.Value?.Generate();
    }
}

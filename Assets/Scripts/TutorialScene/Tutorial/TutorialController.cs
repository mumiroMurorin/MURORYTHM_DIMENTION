using UnityEngine;
using VContainer;
using System.Collections.Generic;
using UniRx;
using System.Linq;
using Tutorial;
using System.Threading;

public class TutorialController : MonoBehaviour, IDisposer
{
    [SerializeReference, SubclassSelector] TutorialActionNode[] actions;
    [SerializeField] SerializeInterface<ITimeGetter> timer;

    public System.Action OnFinishTutorialListener;

    private void Start()
    {
        Initialize();
    }

    private void Initialize()
    {
        for (int i = 0; i < actions.Length - 1; i++)
        {
            actions[i].SetNextNode(actions[i + 1]);
        }

        // 最後にコールバック関数を呼ぶ
        actions.Last()?.SetNextNode(new Callback(() => {
            OnFinishTutorialListener?.Invoke();
        }));

        // ゲームの最初だけ実行
        timer?.Value.TimeRP
            .Where(time => 0 < time)
            .Take(1)
            .Subscribe(_ => OnStartStage())
            .AddTo(this.gameObject);
    }

    private void OnStartStage()
    {
        actions?[0]?.Do();
    }


    List<CancellationTokenSource> ctsList = new List<CancellationTokenSource>();

    public void SetCts(CancellationTokenSource cts)
    {
        ctsList.Add(cts);
    }

    private void OnDestroy()
    {
        foreach(var cts in ctsList)
        {
            cts?.CancelAndDispose();
        }
    }
}

public interface IDisposer
{
    void SetCts(CancellationTokenSource cts);
}
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using UniRx;
using UnityEngine;
using System;  // OperationCanceledException使用のため必要

public class OperationThemeColorSerialBridge : MonoBehaviour
{
    [SerializeField] private SerializeInterface<IOperationGetter> operationGetter_model;
    [SerializeField] private bool applyExistingItemsOnStart = true;
    [SerializeField] private List<SliderIndexToLedMap> sliderIndexToLedMaps = new List<SliderIndexToLedMap>()
    {
        new SliderIndexToLedMap(0,  new List<LedAddress> { new LedAddress(0, 0), new LedAddress(0, 1) }),
        new SliderIndexToLedMap(1,  new List<LedAddress> { new LedAddress(0, 2), new LedAddress(0, 3) }),
        new SliderIndexToLedMap(2,  new List<LedAddress> { new LedAddress(0, 8), new LedAddress(0, 9) }),
        new SliderIndexToLedMap(3,  new List<LedAddress> { new LedAddress(0, 10), new LedAddress(0, 11) }),
        new SliderIndexToLedMap(4,  new List<LedAddress> { new LedAddress(1, 0), new LedAddress(1, 1) }),
        new SliderIndexToLedMap(5,  new List<LedAddress> { new LedAddress(1, 2), new LedAddress(1, 3) }),
        new SliderIndexToLedMap(6,  new List<LedAddress> { new LedAddress(1, 8), new LedAddress(1, 9) }),
        new SliderIndexToLedMap(7,  new List<LedAddress> { new LedAddress(1, 10), new LedAddress(1, 11) }),
        new SliderIndexToLedMap(8,  new List<LedAddress> { new LedAddress(2, 0), new LedAddress(2, 1) }),
        new SliderIndexToLedMap(9,  new List<LedAddress> { new LedAddress(2, 2), new LedAddress(2, 3) }),
        new SliderIndexToLedMap(10, new List<LedAddress> { new LedAddress(2, 8), new LedAddress(2, 9) }),
        new SliderIndexToLedMap(11, new List<LedAddress> { new LedAddress(2, 10), new LedAddress(2, 11) }),
        new SliderIndexToLedMap(12, new List<LedAddress> { new LedAddress(3, 0), new LedAddress(3, 1) }),
        new SliderIndexToLedMap(13, new List<LedAddress> { new LedAddress(3, 2), new LedAddress(3, 3) }),
        new SliderIndexToLedMap(14, new List<LedAddress> { new LedAddress(3, 8), new LedAddress(3, 9) }),
        new SliderIndexToLedMap(15, new List<LedAddress> { new LedAddress(3, 10), new LedAddress(3, 11) }),
    };
    [SerializeField] private int bindWaitTimeoutMs = 3000;

    private readonly CompositeDisposable disposables = new CompositeDisposable();

    private IOperationGetter operationGetter;
    private CancellationToken destroyToken;
    private SerialSender cachedSender;
    private bool refreshRequested;
    private bool isRefreshing;

    private async void Start()
    {
        EnsureDefaultMaps();
        destroyToken = this.GetCancellationTokenOnDestroy();

        try
        {
            await BindAsync(destroyToken);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void Reset()
    {
        EnsureDefaultMaps();
    }

    private void OnValidate()
    {
        EnsureDefaultMaps();
    }

    private async UniTask BindAsync(CancellationToken token)
    {
        await UniTask.Yield(PlayerLoopTiming.Update, token);

        await UniTask.WhenAny(
            UniTask.WaitUntil(() => operationGetter_model?.Value != null, cancellationToken: token),
            UniTask.Delay(bindWaitTimeoutMs, cancellationToken: token));

        operationGetter = operationGetter_model?.Value;
        if (operationGetter == null)
        {
            Debug.LogWarning("【OperationThemeColorSerialBridge】 IOperationGetter was not found.");
            return;
        }

        var sliderTouchDatas = operationGetter.SliderTouchDatas;
        if (sliderTouchDatas == null)
        {
            Debug.LogWarning("【OperationThemeColorSerialBridge】 SliderTouchDatas was not found.");
            return;
        }

        sliderTouchDatas
            .ObserveAdd()
            .Subscribe(_ => RequestRefresh())
            .AddTo(disposables);

        sliderTouchDatas
            .ObserveRemove()
            .Subscribe(_ => RequestRefresh())
            .AddTo(disposables);

        sliderTouchDatas
            .ObserveReset()
            .Subscribe(_ => RequestRefresh())
            .AddTo(disposables);

        if (applyExistingItemsOnStart)
        {
            RequestRefresh();
        }
    }

    private void OnDestroy()
    {
        disposables.Dispose();
    }

    private void RequestRefresh()
    {
        refreshRequested = true;
        if (isRefreshing) { return; }

        RefreshLoopAsync(destroyToken).Forget();
    }

    private async UniTaskVoid RefreshLoopAsync(CancellationToken token)
    {
        isRefreshing = true;
        try
        {
            while (refreshRequested)
            {
                refreshRequested = false;

                // Batch Reset/Add bursts caused by phase changes into a single LED refresh.
                await UniTask.Yield(PlayerLoopTiming.Update, token);
                RefreshCurrentOperations();
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            isRefreshing = false;

            if (refreshRequested && !token.IsCancellationRequested)
            {
                RefreshLoopAsync(token).Forget();
            }
        }
    }

    private void RefreshCurrentOperations()
    {
        var sender = GetConnectedSender();
        if (sender == null) { return; }

        sender.ClearManual();

        if (operationGetter == null) { return; }

        IReadOnlyReactiveCollection<SliderTouchData> sliderTouchDatas = operationGetter.SliderTouchDatas;
        if (sliderTouchDatas == null) { return; }

        foreach (var sliderTouchData in sliderTouchDatas)
        {
            ApplySliderTouchData(sender, sliderTouchData);
        }
    }

    private void ApplySliderTouchData(SerialSender sender, SliderTouchData sliderTouchData)
    {
        if (sender == null) { return; }
        if (sliderTouchData == null) { return; }

        bool controllerRainbow = sliderTouchData.ControllerRainbow;
        Color32 color = sliderTouchData.ControllerColor;
        foreach (int sliderIndex in sliderTouchData.SliderIndices)
        {
            var ledMap = sliderIndexToLedMaps.FirstOrDefault(map => map.SliderIndex == sliderIndex);
            if (ledMap == null) { continue; }

            foreach (var ledAddress in ledMap.LedAddresses)
            {
                if (controllerRainbow)
                {
                    sender.SetMappedRainbow(ledAddress.Channel, ledAddress.Electrode);
                }
                else
                {
                    sender.SetMappedLed(ledAddress.Channel, ledAddress.Electrode, color);
                }
            }
        }
    }

    private SerialSender GetConnectedSender()
    {
        var sender = ResolveSender();
        if (sender == null) { return null; }
        return sender.IsConnected ? sender : null;
    }

    private SerialSender ResolveSender()
    {
        if (cachedSender != null)
        {
            return cachedSender;
        }

        cachedSender = FindObjectOfType<SerialSender>();
        return cachedSender;
    }

    private void EnsureDefaultMaps()
    {
        if (sliderIndexToLedMaps != null && sliderIndexToLedMaps.Count > 0)
        {
            return;
        }

        sliderIndexToLedMaps = new List<SliderIndexToLedMap>()
        {
            new SliderIndexToLedMap(0,  new List<LedAddress> { new LedAddress(0, 0), new LedAddress(0, 1) }),
            new SliderIndexToLedMap(1,  new List<LedAddress> { new LedAddress(0, 2), new LedAddress(0, 3) }),
            new SliderIndexToLedMap(2,  new List<LedAddress> { new LedAddress(0, 8), new LedAddress(0, 9) }),
            new SliderIndexToLedMap(3,  new List<LedAddress> { new LedAddress(0, 10), new LedAddress(0, 11) }),
            new SliderIndexToLedMap(4,  new List<LedAddress> { new LedAddress(1, 0), new LedAddress(1, 1) }),
            new SliderIndexToLedMap(5,  new List<LedAddress> { new LedAddress(1, 2), new LedAddress(1, 3) }),
            new SliderIndexToLedMap(6,  new List<LedAddress> { new LedAddress(1, 8), new LedAddress(1, 9) }),
            new SliderIndexToLedMap(7,  new List<LedAddress> { new LedAddress(1, 10), new LedAddress(1, 11) }),
            new SliderIndexToLedMap(8,  new List<LedAddress> { new LedAddress(2, 0), new LedAddress(2, 1) }),
            new SliderIndexToLedMap(9,  new List<LedAddress> { new LedAddress(2, 2), new LedAddress(2, 3) }),
            new SliderIndexToLedMap(10, new List<LedAddress> { new LedAddress(2, 8), new LedAddress(2, 9) }),
            new SliderIndexToLedMap(11, new List<LedAddress> { new LedAddress(2, 10), new LedAddress(2, 11) }),
            new SliderIndexToLedMap(12, new List<LedAddress> { new LedAddress(3, 0), new LedAddress(3, 1) }),
            new SliderIndexToLedMap(13, new List<LedAddress> { new LedAddress(3, 2), new LedAddress(3, 3) }),
            new SliderIndexToLedMap(14, new List<LedAddress> { new LedAddress(3, 8), new LedAddress(3, 9) }),
            new SliderIndexToLedMap(15, new List<LedAddress> { new LedAddress(3, 10), new LedAddress(3, 11) }),
        };
    }

    [System.Serializable]
    public class SliderIndexToLedMap
    {
        [SerializeField] private int sliderIndex;
        [SerializeField] private List<LedAddress> ledAddresses = new List<LedAddress>();

        public SliderIndexToLedMap(int sliderIndex, List<LedAddress> ledAddresses)
        {
            this.sliderIndex = sliderIndex;
            this.ledAddresses = ledAddresses;
        }

        public int SliderIndex => sliderIndex;
        public IReadOnlyList<LedAddress> LedAddresses => ledAddresses;
    }

    [System.Serializable]
    public class LedAddress
    {
        [SerializeField] private int channel;
        [SerializeField] private int electrode;

        public LedAddress(int channel, int electrode)
        {
            this.channel = channel;
            this.electrode = electrode;
        }

        public int Channel => channel;
        public int Electrode => electrode;
    }
}

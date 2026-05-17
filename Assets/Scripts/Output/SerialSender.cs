using UnityEngine;
using System.IO.Ports;
using System.Text;
using System;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;

public class SerialSender : SingletonMonoBehaviour<SerialSender>
{
    [Header("Serial Settings")]
    [SerializeField] private string portName = "COM5";
    [SerializeField] private int baudRate = 115200;
    [SerializeField] private bool autoConnect = true;
    [SerializeField] private string handshakeRequest = "CONDUCTUS_PING";
    [SerializeField] private string handshakeResponse = "CONDUCTUS_PONG";
    [SerializeField] private int handshakeTimeoutMs = 200;
    [SerializeField] private int openDelayMs = 200;

    private SerialPort serialPort;
    public bool IsConnected => serialPort != null && serialPort.IsOpen;

    private async void Start()
    {
        if (autoConnect)
        {
            await AutoConnectAsync(this.GetCancellationTokenOnDestroy());
        }
        else
        {
            OpenSpecificPort(portName);
        }
    }

    void OnDestroy()
    {
        ClosePort();
    }

    void OnApplicationQuit()
    {
        ClosePort();
    }

    private void ClosePort()
    {
        if (serialPort == null) return;

        ClearManual();

        try
        {
            if (serialPort.IsOpen)
            {
                serialPort.Close();
            }

            serialPort.Dispose();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"【SerialPort】Serial close failed: {e.Message}");
        }
        finally
        {
            serialPort = null;
        }
    }

    private async UniTask AutoConnectAsync(CancellationToken token)
    {
        ClosePort();

        string[] ports = SerialPort.GetPortNames();
        if (ports == null || ports.Length == 0)
        {
            Debug.LogWarning("【SerialPort】Serial port was not found.");
            return;
        }

        foreach (string candidate in ports.OrderBy(p => p))
        {
            token.ThrowIfCancellationRequested();

            SerialPort matchedPort = await UniTask.RunOnThreadPool(
                () => AttemptHandshake(candidate, token),
                cancellationToken: token);

            if (matchedPort != null)
            {
                serialPort = matchedPort;
                portName = candidate;
                await UniTask.Delay(openDelayMs, cancellationToken: token);

                try
                {
                    serialPort.DiscardInBuffer();
                    serialPort.DiscardOutBuffer();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"【SerialPort】Serial buffer clear failed: {e.Message}");
                }

                Debug.Log($"【SerialPort】Serial connected: {candidate}");
                return;
            }
        }

        Debug.LogWarning("【SerialPort】Handshake failed on all ports.");
    }

    private void OpenSpecificPort(string targetPortName)
    {
        try
        {
            ClosePort();

            serialPort = CreateSerialPort(targetPortName);
            serialPort.Open();

            Debug.Log($"【SerialPort】Serial opened: {targetPortName}");
        }
        catch (Exception e)
        {
            Debug.LogError($"【SerialPort】Serial open failed: {e.Message}");
            ClosePort();
        }
    }

    private SerialPort CreateSerialPort(string targetPortName)
    {
        var sp = new SerialPort(targetPortName, baudRate);
        sp.NewLine = "\n";
        sp.Encoding = Encoding.ASCII;
        sp.ReadTimeout = handshakeTimeoutMs;
        sp.WriteTimeout = handshakeTimeoutMs;
        sp.DtrEnable = true;
        sp.RtsEnable = true;
        return sp;
    }

    private SerialPort AttemptHandshake(string candidatePortName, CancellationToken token)
    {
        SerialPort sp = null;

        try
        {
            sp = CreateSerialPort(candidatePortName);
            sp.Open();
            sp.DiscardInBuffer();
            sp.DiscardOutBuffer();

            sp.WriteLine(handshakeRequest);

            var deadline = DateTime.UtcNow + TimeSpan.FromMilliseconds(handshakeTimeoutMs);
            while (DateTime.UtcNow < deadline)
            {
                token.ThrowIfCancellationRequested();

                try
                {
                    string reply = sp.ReadLine()?.Trim();
                    if (string.Equals(reply, handshakeResponse, StringComparison.OrdinalIgnoreCase))
                    {
                        return sp;
                    }
                }
                catch (TimeoutException)
                {
                    // continue waiting until deadline
                }
            }
        }
        catch
        {
            // handshake failed
        }

        if (sp != null)
        {
            try
            {
                if (sp.IsOpen)
                {
                    sp.Close();
                }
            }
            catch
            {
                // ignore cleanup errors
            }

            sp.Dispose();
        }

        return null;
    }

    private void SendRaw(string message)
    {
        if (serialPort == null || !serialPort.IsOpen)
        {
            Debug.LogWarning("【SerialPort】Serial port is not open.");
            return;
        }

        try
        {
            serialPort.WriteLine(message);
            Debug.Log($"【SerialPort】Sent: {message}");
        }
        catch (Exception e)
        {
            Debug.LogError($"【SerialPort】Serial send failed: {e.Message}");
        }
    }

    public void SetLed(int ledIndex, Color color)
    {
        SetLed(ledIndex, (Color32)color);
    }

    public void SetLed(int ledIndex, Color32 color)
    {
        SendRaw($"L {ledIndex} {color.r} {color.g} {color.b}");
    }

    public void SetAllLed(Color color)
    {
        SetAllLed((Color32)color);
    }

    public void SetAllLed(Color32 color)
    {
        SendRaw($"A {color.r} {color.g} {color.b}");
    }

    public void ClearManual()
    {
        SendRaw("X");
    }

    /// <summary>
    /// LED_MAP の「n番チャンネルのm番電極」に対応するLEDをcolor色にする
    /// </summary>
    public void SetMappedLed(int channel, int electrode, Color color)
    {
        SetMappedLed(channel, electrode, (Color32)color);
    }

    public void SetMappedLed(int channel, int electrode, Color32 color)
    {
        SendRaw($"C {channel} {electrode} {color.r} {color.g} {color.b}");
    }

    public void SetMappedRainbow(int channel, int electrode)
    {
        SendRaw($"R {channel} {electrode}");
    }
}

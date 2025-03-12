using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.IO.Ports;
using System.Threading;
using System;
using System.Runtime.InteropServices;
using System.Text;

public class SerialHandler : MonoBehaviour
{
    [SerializeField] private SliderInput slider;
    public string portName = "COM7";
    public int baudRate = 9600;

    private SerialPort serialPort_;
    private Thread thread_;
    private bool isRunning_ = false;
    private string lastrcvd = "";

    private string message_;
    private bool isNewMessageReceived_ = false;

    void Start()
    {

    }

    void Update()
    {
        /*
        if (isNewMessageReceived_)
        {
            OnDataReceived(message_);
        }*/
    }

    void OnDestroy()
    {
        Close();
    }

    private void Open()
    {
        serialPort_ = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        serialPort_.ReadTimeout = 5000;

        serialPort_.Open();

        isRunning_ = true;

        thread_ = new Thread(Read);
        thread_.Start();
    }

    private void Read()
    {
        byte rcv;
        char tmp;

        while (isRunning_ && serialPort_ != null && serialPort_.IsOpen)
        {
            try
            {
                rcv = (byte)serialPort_.ReadByte();

                if (rcv == '\r')
                {
                    message_ = lastrcvd;
                    //Debug.LogFormat("textLine:{0}", message_);
                    lastrcvd = "";
                    isNewMessageReceived_ = true;
                }
                else
                {
                    tmp = (char)rcv;
                    //Debug.LogFormat("rcv:{0}", tmp.ToString());
                    lastrcvd = lastrcvd + tmp.ToString();
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning(e.Message);
            }
        }
    }

    private void Close()
    {
        isRunning_ = false;

        if (thread_ != null && thread_.IsAlive)
        {
            thread_.Join();
        }

        if (serialPort_ != null && serialPort_.IsOpen)
        {
            serialPort_.Close();
            serialPort_.Dispose();
        }
    }

    void OnDataReceived(string message_)
    {
        string data = message_.Split('_')[1];
        if (data.Length < 3) return;
        
        /*
        try
        {
            Debug.LogFormat("0:{0} 1:{1} 2:{2}", data[0], data[1], data[2]);
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(e.Message);
        }
        */
    }

    //ポート番号とスピード(レート)をセット
    public void SetOption(string str,int speed)
    {
        portName = str;
        baudRate = speed;
    }

    //受信スタート
    public void StartSerial()
    {
        if (slider.IsReturnSerialMode())
        {
            lastrcvd = "";
            Open();
        }
    }

    //受信データを返す
    public string ReturnTouchData()
    {
        if (!isNewMessageReceived_) { return null; }
        return message_;
    }

    //接続されているかどうか返す
    public bool IsReturnConnectDevice()
    {
        return isRunning_ && serialPort_ != null && serialPort_.IsOpen;
    }
}
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO.Ports;
using System.Threading;
using System;

public class Lab : MonoBehaviour
{
    public TextMeshProUGUI TMP;
    public string portName = "COM7";
    public int baudRate = 115200;

    private SerialPort serialPort_;
    private Thread thread_;
    private bool isRunning_ = false;
    private string lastrcvd = "";
    byte rcv;
    char tmp;

    private string message_;
    private bool isNewMessageReceived_ = false;

    void Start()
    {
        lastrcvd = "";
        Open();
    }

    void Update()
    {
        if (isNewMessageReceived_)
        {
            //OnDataReceived(message_);
            TMP.text = tmp.ToString();
        }
    }

    void OnDestroy()
    {
        Debug.Log("close");
        Close();
    }

    private void Open()
    {
        TMP.text = portName + "," + baudRate;

        try
        {
            serialPort_ = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
        }
        catch (System.Exception e)
        {
            TMP.text = e.Message;
        }

        try
        {
            serialPort_.Open();
            serialPort_.ReadTimeout = 5000;
            TMP.text = "[2]";
        }
        catch (Exception e)
        {
            serialPort_ = null;
            return;
        }

        TMP.text = "[3]";

        isRunning_ = true;

        thread_ = new Thread(Read);
        thread_.Start();
    }

    private void Read()
    {
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

    private void ReadLine()
    {
        string rcv;
        char tmp;

        while (isRunning_ && serialPort_ != null && serialPort_.IsOpen)
        {
            Debug.Log("‚í‚Ÿ");
            try
            {
                rcv = serialPort_.ReadLine();
                Debug.Log(rcv);
                //rcv = (byte)serialPort_.ReadLine();

                /*
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
                }*/
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
            Debug.Log("close2");
            serialPort_.Close();
            serialPort_.Dispose();
        }
    }

    void OnDataReceived(string message_)
    {
        //string data = message_.Split('_')[1];
        //if (data.Length < 3) return;

        TMP.text = message_;

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
}
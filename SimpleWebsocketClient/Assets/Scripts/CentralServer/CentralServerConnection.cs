using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketIO;
using WebSocketSharp;

/// <summary>
/// Convenience wrapper to handle and register callbacks from SocketIOComponent
/// </summary>
[RequireComponent(typeof(SocketIOComponent))]
public class CentralServerConnection : MonoBehaviour {

    public delegate void ConnectionCallback();
    public delegate void ConnectionCallbackString(string data);

    public ConnectionCallback OnConnectCallback;
    public ConnectionCallback OnDisconnectCallback;
    public ConnectionCallback OnErrorCallback;

    private SocketIOComponent m_Socket = null;
    private Dictionary<string, Action<string>> m_RegisteredCallbacks = new Dictionary<string, Action<string>>();

    private void Awake()
    {
        m_Socket = this.GetComponent<SocketIOComponent>();

        //register handlers
        m_Socket.On("connect", OnConnect);
        m_Socket.On("disconnect", OnDisconnect);
        m_Socket.On("error", OnError);
        m_Socket.On("ConnectionID", OnConnectionID);
    }

    private void OnEnable()
    {
        m_Socket.OnHandlePacket += OnHandlePacket;
    }

    private void OnDisable()
    {
        m_Socket.OnHandlePacket -= OnHandlePacket;
    }

    public void Connect()
    {
        m_Socket.Connect();
    }

    public void Disconnect()
    {
        m_Socket.Close();
    }

    public void RegisterCustomCallback(string key, Action<string> callback)
    {
        if (!m_RegisteredCallbacks.ContainsKey(key))
        {
            m_RegisteredCallbacks.Add(key, callback);
        }
        else
        {
            Debug.LogWarning("Callback already registered for key: " + key + ". Overwriting..");
            m_RegisteredCallbacks[key] = callback;
        }
    }

    public void DeregisterCustomCallback(string key)
    {
        if (!m_RegisteredCallbacks.ContainsKey(key))
        {
            Debug.LogWarning("No callback registered for key: " + key);
        }
        else
        {
            m_RegisteredCallbacks.Remove(key);
        }
    }

    public void SendToServer(string key, string msg = null)
    {
        if (msg != null)
        {
            var msgJSON = JSONObject.StringObject(msg); //send all socket data as a json object
            m_Socket.Emit(key, msgJSON);
        }
        else
        {
            m_Socket.Emit(key);
        }
    }

    private void OnConnect(SocketIO.SocketIOEvent e)
    {
        Debug.Log("Connection established");
        if (OnConnectCallback != null)
        {
            OnConnectCallback();
        }
    }

    private void OnDisconnect(SocketIO.SocketIOEvent e)
    {
        Debug.Log("Connection closed");
        if (OnDisconnectCallback != null)
        {
            OnDisconnectCallback();
        }
    }

    private void OnError(SocketIO.SocketIOEvent e)
    {
        Debug.LogWarning("WebSocket error.");
        if (OnErrorCallback != null)
        {
            OnErrorCallback();
        }
    }

    private void OnConnectionID(SocketIOEvent e)
    {
        Debug.Log("connectionid: " + e.data[0].ToString());
    }

    private void OnHandlePacket(Packet packet)
    {
        // get msg type
        var msgType = RemoveFirstAndLastChar(packet.json.list[0].ToString()); // get msg key and remove quotations
        Debug.Log("Packet message type: " + msgType);

        var stringData = RemoveFirstAndLastChar(packet.json.list[1].ToString());

        if (m_RegisteredCallbacks.ContainsKey(msgType))
        {
            if (m_RegisteredCallbacks[msgType] != null)
            {
                m_RegisteredCallbacks[msgType](stringData);
            }
        }
        else
        {
            Debug.LogWarning("No callback registered for key: " + msgType);
        }

        //if (msgType == "ConnectionID")
        //{
        //    var rawMsg = packet.json.list[1].ToString();
        //    var connectionID = rawMsg.Substring(1, rawMsg.Length - 2);

        //    Debug.Log("connectionID: " + connectionID);
        //}
    }

    private void Update()
    {
    }

    private string RemoveFirstAndLastChar(string str)
    {
        if (str.Length >= 4)
        {
            return str.Substring(1, str.Length - 2);
        }
        else
        {
            Debug.LogWarning("Invalid str. Too short.");
            return str;
        }
    }
}

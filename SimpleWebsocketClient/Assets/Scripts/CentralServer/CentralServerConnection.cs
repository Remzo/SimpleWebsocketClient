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
    public delegate void ConnectionCallbackBool(bool val);
    public delegate void ConnectionCallbackString(JSONObject data);

    public ConnectionCallbackBool OnConnectCallback;
    public ConnectionCallback OnDisconnectCallback;
    public ConnectionCallback OnErrorCallback;

    private SocketIOComponent m_Socket = null;
    private Dictionary<string, Action<JSONObject>> m_RegisteredCallbacks = new Dictionary<string, Action<JSONObject>>();

    private void Awake()
    {
        m_Socket = this.GetComponent<SocketIOComponent>();

        //register handlers
        m_Socket.On("connect", OnConnect);
        m_Socket.On("disconnect", OnDisconnect);
        m_Socket.On("error", OnError);
        m_Socket.On("ConnectionFailed", OnConnectionFailed);
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

    public void RegisterCustomCallback(string key, Action<JSONObject> callback)
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
            OnConnectCallback(true);
        }
    }

    private void OnConnectionFailed(SocketIOEvent e)
    {
        Debug.Log("Connection failed.");
        if (OnConnectCallback != null)
        {
            OnConnectCallback(false);
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
        //Debug.LogWarning("WebSocket error.");
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

        //check if a callback has been registered for the message type
        if (m_RegisteredCallbacks.ContainsKey(msgType))
        {
            if (m_RegisteredCallbacks[msgType] != null)
            {
                Debug.Log("Callback for " + msgType);
                m_RegisteredCallbacks[msgType](packet.json); //trigger callback
            }
        }
        else
        {
            Debug.LogWarning("No callback registered for key: " + msgType);
        }
    }

    //Utility function to remove the first and last characters of a string. Used to remove quotations marks from packet json data
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

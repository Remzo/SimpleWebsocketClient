using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Controller : MonoBehaviour {

    [SerializeField]
    private CentralServerConnection m_CentralServer = null;

    [Header("UI Common")]
    [SerializeField]
    private GameObject m_Prompt = null;
    [SerializeField]
    private GameObject m_Common = null;
    [SerializeField]
    private Text m_MessageBox = null;
    [SerializeField]
    private Text m_DataBox = null;
    [Header("UI Sender")]
    [SerializeField]
    private GameObject m_Sender = null;
    [SerializeField]
    private Text m_SenderMessageBox = null;
    [SerializeField]
    private Text m_SenderConnectionID = null;
    [Header("UI Receiver")]
    [SerializeField]
    private GameObject m_Receiver = null;
    [SerializeField]
    private Text m_ReceiverMessageBox = null;
    [SerializeField]
    private GameObject m_GetDataButton = null;

    private bool b_CallbacksRegistered = false;

    private bool b_Connected = false;
    private bool b_IsSender = false;

    private bool b_ConnectionIDReceived = false;
    private bool b_ItemDataRecieved = false;

    private string m_ItemID = "";
    private string m_ConnectionID = "";

	// Use this for initialization
	void Start ()
    {
	}

    private void OnEnable()
    {
        if (!b_CallbacksRegistered)
        {
            RegisterCallbacks();
        }
    }

    private void OnDisable()
    {
        if (b_CallbacksRegistered)
        {
            DeregisterCallbacks();
        }
        StopAllCoroutines();
    }

    private void RegisterCallbacks()
    {
        b_CallbacksRegistered = true;
        m_CentralServer.OnConnectCallback += OnConnected;
        m_CentralServer.OnDisconnectCallback += OnDisconnect;
        m_CentralServer.OnErrorCallback += OnError;

        m_CentralServer.RegisterCustomCallback("ConnectionID", OnConnectionIDReceived);
        m_CentralServer.RegisterCustomCallback("ItemID", OnItemIDReceived);
        m_CentralServer.RegisterCustomCallback("ItemReceived", OnItemReceived);
    }

    private void DeregisterCallbacks()
    {
        b_CallbacksRegistered = false;
        m_CentralServer.OnConnectCallback -= OnConnected;
        m_CentralServer.OnDisconnectCallback -= OnDisconnect;
        m_CentralServer.OnErrorCallback -= OnError;

        m_CentralServer.DeregisterCustomCallback("ConnectionID");
        m_CentralServer.DeregisterCustomCallback("ItemID");
        m_CentralServer.DeregisterCustomCallback("ItemReceived");
    }

    private void OnConnected()
    {
        //handle state on connect
        b_Connected = true;
        if (b_IsSender)
        {
            m_CentralServer.SendToServer("Host", m_ItemID); //send host message and itemID to server to receive connection ID
            StartCoroutine(WaitForConnectionID()); //start coroutine to wait for connection id
            StartCoroutine(WaitForItemReceived());//start coroutine to wait for item received
        }
        else
        {
            m_CentralServer.SendToServer("GetItem", m_ConnectionID);
            StartCoroutine(WaitForItemID());
        }
    }

    private void OnDisconnect()
    {
        //handle state on disconnect
        Debug.Log("Disconnected");
    }

    private void OnError()
    {
        //handle state on error
    }

    private void OnConnectionIDReceived(string connectionID)
    {
        Debug.Log("ConnectionID: " + connectionID);

        m_ConnectionID = connectionID; //set connection ID for display
        b_ConnectionIDReceived = true; //flag received for coroutine
    }

    private IEnumerator WaitForConnectionID()
    {
        while (!b_ConnectionIDReceived)
        {
            yield return new WaitForEndOfFrame();
        }

        //display connection id
        m_SenderConnectionID.text = m_ConnectionID;

        //reset flag
        b_ConnectionIDReceived = false;

        yield return null;
    }

    private void OnItemReceived(string msg)
    {
        //item received by reciever
        b_ItemDataRecieved = true;

        Debug.Log("Item received by receiver.");
    }

    private IEnumerator WaitForItemReceived()
    {
        while (!b_ItemDataRecieved)
        {
            yield return new WaitForEndOfFrame();
        }

        //close connection
        Disconnect();

        b_ItemDataRecieved = false;
    }

    public void SetConnectionID(string connID)
    {
        m_ConnectionID = connID.ToUpper();
    }

    public void GetItemFromConnectionID()
    {
        if (!IsOnline())
        {
            m_DataBox.text = "Please connect to the internet.";
            return;
        }

        if (m_ConnectionID == "") //connection ID validation goes here
        {
            Debug.LogWarning("Input valid connection id");
            return;
        }

        m_CentralServer.Connect();
        m_GetDataButton.SetActive(false);
    }

    private void OnItemIDReceived(string itemID)
    {
        Debug.Log("ItemID: " + itemID);

        m_ItemID = itemID; //set item ID for display
        b_ItemDataRecieved = true; //flag received for coroutine
    }

    private IEnumerator WaitForItemID()
    {
        while (!b_ItemDataRecieved)
        {
            yield return new WaitForEndOfFrame();
        }
        b_ItemDataRecieved = false;

        if (m_ItemID == "INVALID") //item id validation here
        {
            m_DataBox.text = "Invalid connection ID";
            m_GetDataButton.SetActive(true);
        }
        else
        {
            m_DataBox.text = "Data: " + m_ItemID;
            m_CentralServer.SendToServer("ItemReceived", m_ConnectionID);
        }

        Disconnect();//disconnect from central server
    }

    public void SetItemUpForGrabs(string itemID)
    {
        m_ItemID = itemID;
    }

    public void PutItemUpForGrabs()
    {
        if (!IsOnline())
        {
            m_MessageBox.text = "Please connect to the internet.";
            return;
        }

        if (m_ItemID == "")
        {
            m_MessageBox.text = "Enter an itemID to send";
            return;
        }
        b_IsSender = true;
        m_CentralServer.Connect();

        //init UI
        m_Prompt.SetActive(false);
        m_Sender.SetActive(true);
        m_Common.SetActive(true);
        m_Receiver.SetActive(false);
        m_SenderMessageBox.text = "";
        m_DataBox.text = "Data: " + m_ItemID;
        m_SenderConnectionID.text = "";
    }

    public void InitReceiver()
    {
        b_IsSender = false;
        m_Prompt.SetActive(false);
        m_Sender.SetActive(false);
        m_Receiver.SetActive(true);
        m_Common.SetActive(true);
        m_GetDataButton.SetActive(true);
    }

    public void BackToPrompt()
    {
        Disconnect();
        StopAllCoroutines();
        m_Prompt.SetActive(true);
        m_Sender.SetActive(false);
        m_Common.SetActive(false);
        m_Receiver.SetActive(false);
        m_MessageBox.text = "";
        m_DataBox.text = "";
    }

    private bool IsOnline()
    {
        return Application.internetReachability != NetworkReachability.NotReachable;
    }

    private void Disconnect()
    {
        if (b_Connected)
        {
            b_Connected = false;
            StopAllCoroutines();
            m_CentralServer.Disconnect();
        }
    }
}

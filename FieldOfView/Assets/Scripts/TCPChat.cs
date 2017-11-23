using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace HD
{
  /// Multithreading (may lead to a lot of mostly-idle threads), 
  /// non-blocking, 
  /// select (rec select or non-blocking on single thread)

  /// DNS
  //IPHostEntry ipHost = Dns.Resolve("google.com");
  //ipHost.AddressList[0];

  /// Select
  //List<Socket> listOfSocketsToCheckForRead = new List<Socket>();
  //listOfSocketsToCheckForRead.Add(tcpClient.Client);
  //Socket.Select(listOfSocketsToCheckForRead, null, null, 0);
  //for(int i = 0; i < listOfSocketsToCheckForRead.Count; i++)
  //{
  //  listOfSocketsToCheckForRead[i].Receive(...);
  //}




  public class TCPChat : MonoBehaviour
  {
    #region Data
    public static TCPChat instance;

    public bool isServer;

        /// <summary>
        /// IP for clients to connect to. Null if you are the server.
        /// </summary>
    public IPAddress serverIp;
    public int port = 5000;
    public string address = "192.168.0.227";
    /// <summary>
    /// For Clients, there is only one and it's the connection to the server.
    /// For Servers, there are many - one per connected client.
    /// </summary>
    List<TcpConnectedClient> clientList = new List<TcpConnectedClient>();
    Queue<byte[]> messageQueue = new Queue<byte[]>();
    /// <summary>
    /// The string to render in Unity.
    /// </summary>
        public static string messageToDisplay;
    public Text text;
  
    
    /// <summary>
    /// Accepts new connections.  Null for clients.
    /// </summary>
    TcpListener listener;
    ProtocolClient protocolClient;
    #endregion

    #region Unity Events
    public void Awake()
    {
      instance = this;
      
      if(serverIp == null)
      { // Server: start listening for connections
        this.isServer = true;
        listener = new TcpListener(IPAddress.Parse(address), port);
        //listener = new TcpListener(IPAddress.Parse("127.0.0.1"), port);
        //listener = new TcpListener(localaddr: IPAddress.Any, port: port);
        listener.Start();
        listener.BeginAcceptTcpClient(OnServerConnect, null);
      }
      else
      { // Client: try connecting to the server
        TcpClient client = new TcpClient();
        TcpConnectedClient connectedClient = new TcpConnectedClient(client);
        clientList.Add(connectedClient);
        client.BeginConnect(serverIp, port, (ar) => connectedClient.EndConnect(ar), null);
      }
    }
        void Start()
        {
            protocolClient = GetComponent<ProtocolClient>();
        }

            protected void OnApplicationQuit()
    {
            if (listener != null) {
                listener.Stop();
            }
      //listener?.Stop();
      for(int i = 0; i < clientList.Count; i++)
      {
        clientList[i].Close();
      }
    }

    protected void Update()
    {
            print(messageQueue.Count);
            if (clientList.Count > 0) {
                while(messageQueue.Count > 0)
                {
                    Send(messageQueue.Dequeue());
                }
            }
            if (messageToDisplay != null)
            {
                //text.text = messageToDisplay;
            }
    }
    #endregion

    #region Async Events
    void OnServerConnect(IAsyncResult ar)
    {
            print("client connect");
            TcpClient tcpClient = listener.EndAcceptTcpClient(ar);
            clientList.Add(new TcpConnectedClient(tcpClient));

            listener.BeginAcceptTcpClient(OnServerConnect, null);
    }
    #endregion

    #region API
    public void OnDisconnect(TcpConnectedClient client)
    {
            print("client disconnect");
      clientList.Remove(client);
    }
    public void OnRead(TcpConnectedClient client, byte[] data){
            protocolClient.onHandleMessage(data);
    }
        internal void Send(string message)
        {
          BroadcastChatMessage(message);

          if(isServer)
          {
            messageToDisplay += message + Environment.NewLine;
          }
        }
        internal void Send(byte [] message)
        {
            BroadcastChatMessage(message);

            if (isServer)
            {
                messageToDisplay += message + Environment.NewLine;
            }
        }

        internal static void BroadcastChatMessage(string message)
        {
          for(int i = 0; i < instance.clientList.Count; i++)
          {
            TcpConnectedClient client = instance.clientList[i];
            client.Send(message);
          }
        }
        internal static void BroadcastChatMessage(byte[] message)
        {
            if(instance.clientList.Count == 0)
            {
                instance.messageQueue.Enqueue(message);
            }
            for (int i = 0; i < instance.clientList.Count; i++)
            {
                TcpConnectedClient client = instance.clientList[i];
                client.Send(message);
            }
        }
        #endregion
    }
}

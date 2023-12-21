using Riptide;
using Riptide.Utils;
using System;
using UnityEngine;

public enum ServerToClientId : ushort
{
    session = 1,
    playerJoined = 2,
    startGame = 3,
    playerData = 4,
    playerAction = 5,
    newEnemy = 6,
    updateEnemySpeed = 7,
    enemyDead = 8,
    gameOver = 9,
    updateRestartCount = 10,
}

public enum ClientToServerId : ushort
{
    isReady = 1,
    playerData = 2,
    playerAction = 3,
    enemyHurt = 4,
    updateRestartCount = 5,
}

public class NetworkManager : MonoBehaviour
{
    private static NetworkManager _singleton;

    public static NetworkManager Singleton
    {
        get => _singleton;
        private set
        {
            if (_singleton == null)
            {
                _singleton = value;
            } else if (_singleton != value)
            {
                Debug.Log($"{nameof(NetworkManager)} instance already exists, destroying duplicate!");
                Destroy(value);
            }
        }
    }

    public Server Server { get; private set; }

    [SerializeField] private ushort port;
    [SerializeField] private ushort maxClientCount;

    private static Session session;

    private void Awake()
    {
        Singleton = this;
    }

    private void Start()
    {
        Application.targetFrameRate = 60;
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, false);

        Server = new Server();
        Server.Start(port, maxClientCount);
        Server.ClientConnected += PlayerJoined;
        Server.ClientDisconnected += PlayerLeft;

        session = null;
    }

    private void FixedUpdate()
    {
        session?.Update();

        Server.Update();
    }

    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    private void PlayerJoined(object sender, ServerConnectedEventArgs e)
    {
        e.Client.CanTimeout = false;
        session ??= new Session(1);

        session.AddPlayer(e.Client.Id);
    }

    private void PlayerLeft(object sender, ServerDisconnectedEventArgs e)
    {
        session = null;
    }

    public void SendToAllInSession(ushort sessionId, Message message)
    {
        session?.SendToAll(message);
    }

    [MessageHandler((ushort)(ClientToServerId.isReady))]
    private static void PlayerReady(ushort fromClientId, Message message)
    {
        session.SetReady(fromClientId);

        if (session.IsReady())
        {
            session.StartGame();
        }
    }

    [MessageHandler((ushort)(ClientToServerId.playerData))]
    private static void UpdatePlayerData(ushort fromClientId, Message message)
    {
        Vector3 position = message.GetVector3();

        session?.UpdatePlayerPosition(fromClientId, position);
    }

    [MessageHandler((ushort)ClientToServerId.playerAction)]
    private static void HandlePlayerAction(ushort fromClientId, Message message)
    {
        ushort action = message.GetUShort();

        session?.HandlePlayerAction(fromClientId, action);
    }

    [MessageHandler((ushort)ClientToServerId.enemyHurt)]
    private static void HandleEnemyHurt(ushort _, Message message)
    {
        Guid guid = new(message.GetString());

        session.HandleEnemyHurt(guid);
    }

    [MessageHandler((ushort)ClientToServerId.updateRestartCount)]
    private static void UpdateRestartCound(ushort _, Message message)
    {
        bool wantsRestart = message.GetBool();

        session?.HandleReadyToRestart(wantsRestart);
    }
}

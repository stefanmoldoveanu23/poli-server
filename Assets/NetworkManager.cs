using Riptide;
using Riptide.Utils;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum SessionType : ushort
{
    pub = 1,
    priv = 2,
}

public enum ServerToClientId : ushort
{
    session = 1,
    wrongCode = 2,
    playerJoined = 3,
    startGame = 4,
    playerData = 5,
    playerAction = 6,
    newEnemy = 7,
    updateEnemySpeed = 8,
    enemyDead = 9,
    gameOver = 10,
    updateRestartCount = 11,
    endSession = 12,
}

public enum ClientToServerId : ushort
{
    joinSession = 1,
    isReady = 2,
    playerData = 3,
    playerAction = 4,
    enemyHurt = 5,
    updateRestartCount = 6,
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

    Guid? publicSessionWaiting;
    private Dictionary<Guid, Session> sessions;
    private Dictionary<ushort, Guid> playerSession;

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

        publicSessionWaiting = null;
        sessions = new Dictionary<Guid, Session>();
        playerSession = new Dictionary<ushort, Guid>();
    }

    private void FixedUpdate()
    {
        foreach ((Guid _, Session session) in sessions)
        {
            session?.Update();
        }

        Server.Update();
    }

    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    private Session GetSession(ushort playerId)
    {
        return sessions[playerSession[playerId]];
    }

    private void PlayerJoined(object sender, ServerConnectedEventArgs e)
    {
        e.Client.CanTimeout = false;
    }

    private void PlayerLeft(object sender, ServerDisconnectedEventArgs e)
    {
        Guid sessionId = playerSession[e.Client.Id];
        if (sessions.ContainsKey(sessionId))
        {
            Message message = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.endSession);
            SendToOtherInSession(sessionId, e.Client.Id, message);

            sessions[sessionId] = null;
            sessions.Remove(sessionId);
        }

        playerSession.Remove(e.Client.Id);
    }

    public void SendToOtherInSession(Guid sessionId, ushort playerId, Message message)
    {
        if (sessions.ContainsKey(sessionId))
        {
            sessions[sessionId]?.SendToOther(playerId, message);
        }
    }

    public void SendToAllInSession(Guid sessionId, Message message)
    {
        if (sessions.ContainsKey(sessionId))
        {
            sessions[sessionId]?.SendToAll(message);
        }
    }

    [MessageHandler((ushort)ClientToServerId.joinSession)]
    private static void JoinSession(ushort fromClientId, Message message)
    {
        SessionType type = (SessionType)message.GetUShort();

        switch (type)
        {
            case SessionType.pub:
                {
                    if (Singleton.publicSessionWaiting.HasValue)
                    {
                        Singleton.playerSession.Add(fromClientId, Singleton.publicSessionWaiting.Value);
                        Singleton.sessions[Singleton.publicSessionWaiting.Value].AddPlayer(fromClientId);
                        Singleton.publicSessionWaiting = null;
                    }
                    else
                    {
                        Singleton.publicSessionWaiting = Guid.NewGuid();
                        Singleton.playerSession.Add(fromClientId, Singleton.publicSessionWaiting.Value);
                        Singleton.sessions.Add(Singleton.publicSessionWaiting.Value, new Session(Singleton.publicSessionWaiting.Value));
                        Singleton.sessions[Singleton.publicSessionWaiting.Value].AddPlayer(fromClientId);
                    }

                    break;
                }
            case SessionType.priv:
                {
                    if (message.UnreadLength > 0)
                    {
                        bool isValid = Guid.TryParse(message.GetString(), out Guid sessionId);

                        if (isValid && Singleton.sessions.ContainsKey(sessionId))
                        {
                            Singleton.playerSession.Add(fromClientId, sessionId);
                            Singleton.sessions[sessionId].AddPlayer(fromClientId);
                        }
                        else
                        {
                            Message wrongCode = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.wrongCode);
                            Singleton.Server.Send(wrongCode, fromClientId);
                        }
                    }
                    else
                    {
                        Guid sessionId = Guid.NewGuid();
                        Singleton.playerSession.Add(fromClientId, sessionId);
                        Singleton.sessions.Add(sessionId, new Session(sessionId));
                        Singleton.sessions[sessionId].AddPlayer(fromClientId);
                    }

                    break;
                }
            default:
                {
                    Debug.Log("(SERVER): Wrong session type.");
                    break;
                }
        }
        
    }

    [MessageHandler((ushort)(ClientToServerId.isReady))]
    private static void PlayerReady(ushort fromClientId, Message _)
    {
        Session session = Singleton.GetSession(fromClientId);

        session.SetReady(fromClientId);

        if (session.IsReady())
        {
            session.StartGame();
        }
    }

    [MessageHandler((ushort)(ClientToServerId.playerData))]
    private static void UpdatePlayerData(ushort fromClientId, Message message)
    {
        Session session = Singleton.GetSession(fromClientId);
        Vector3 position = message.GetVector3();

        session?.UpdatePlayerPosition(fromClientId, position);
    }

    [MessageHandler((ushort)ClientToServerId.playerAction)]
    private static void HandlePlayerAction(ushort fromClientId, Message message)
    {
        Session session = Singleton.GetSession(fromClientId);
        ushort action = message.GetUShort();

        session?.HandlePlayerAction(fromClientId, action, message);
    }

    [MessageHandler((ushort)ClientToServerId.enemyHurt)]
    private static void HandleEnemyHurt(ushort fromClientId, Message message)
    {
        Session session = Singleton.GetSession(fromClientId);
        Guid guid = new(message.GetString());

        session?.HandleEnemyHurt(guid);
    }

    [MessageHandler((ushort)ClientToServerId.updateRestartCount)]
    private static void UpdateRestartCound(ushort fromClientId, Message message)
    {
        Session session = Singleton.GetSession(fromClientId);
        bool wantsRestart = message.GetBool();

        session?.HandleReadyToRestart(wantsRestart);
    }
}

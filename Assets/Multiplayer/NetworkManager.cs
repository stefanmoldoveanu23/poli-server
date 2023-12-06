using Riptide;
using Riptide.Utils;
using UnityEngine;

public enum ServerToClientId : ushort
{
    session = 1,
    playerJoined = 2,
    startGame = 3,
    playerData = 4,
}

public enum ClientToServerId : ushort
{
    isReady = 1,
    playerData = 2,
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

    private static ushort? Session { get; set; }
    private static Player player1;
    private static Player player2;
    private static ushort playersReady;

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

        Session = null;
        player1 = null;
        player2 = null;
        playersReady = 0;
    }

    private void FixedUpdate()
    {
        if (playersReady == 2)
        {
            player1.SendPosition();
            player2.SendPosition();
        }
        Server.Update();
    }

    private void OnApplicationQuit()
    {
        Server.Stop();
    }

    private void PlayerJoined(object sender, ServerConnectedEventArgs e)
    {
        e.Client.CanTimeout = false;
        if (Session == null)
        {
            Session = 1;
            player1 = new Player(e.Client.Id);

            Message sendSession = Message.Create(MessageSendMode.Reliable, (ushort)(ServerToClientId.session));
            sendSession.AddUShort(Session.Value);

            Server.Send(sendSession, e.Client.Id);
            Debug.Log($"Player1 joined with id {e.Client.Id}!");
        }
        else
        {
            Message sendSession = Message.Create(MessageSendMode.Reliable, (ushort)(ServerToClientId.session));
            sendSession.AddUShort(Session.Value);

            Server.Send(sendSession, e.Client.Id);

            Message notifyPlayer1 = Message.Create(MessageSendMode.Reliable, (ushort)(ServerToClientId.playerJoined));
            notifyPlayer1.AddUShort(e.Client.Id);
            Server.Send(notifyPlayer1, player1.id);

            player2 = new Player(e.Client.Id);
            Debug.Log($"Player2 joined with id {e.Client.Id}!");

            Message notifyPlayer2 = Message.Create(MessageSendMode.Reliable, (ushort)(ServerToClientId.playerJoined));
            notifyPlayer2.AddUShort(Session.Value);
            Server.Send(notifyPlayer2 , player2.id);
        }
    }

    private void PlayerLeft(object sender, ServerDisconnectedEventArgs e)
    {

    }

    [MessageHandler((ushort)(ClientToServerId.isReady))]
    private static void PlayerReady(ushort fromClientId, Message message)
    {
        ++playersReady;

        if (playersReady == 2)
        {
            Message startGame = Message.Create(MessageSendMode.Reliable, (ushort)(ServerToClientId.startGame));
            startGame.AddUShort(Session.Value);
            Singleton.Server.SendToAll(startGame);
        }
    }

    [MessageHandler((ushort)(ClientToServerId.playerData))]
    private static void UpdatePlayerData(ushort fromClientId, Message message)
    {
        Vector3 position = message.GetVector3();

        if (fromClientId == player1.id)
        {
            player1.UpdatePosition(position);
        }
        else
        {
            player2.UpdatePosition(position);
        }
    }
}

using Riptide;
using UnityEngine;

public enum PlayerActions: ushort
{
    gotHit = 1,
    died = 2,
}

public class Player
{
    public readonly ushort id;
    private Vector3 position;
    public bool IsReady { get; private set; }

    public bool Dead { get; private set; }

    public Player(ushort id)
    {
        this.id = id;
        position = Vector3.zero;
        IsReady = false;

        Debug.Log($"(PLAYER): Player joined with id {this.id}.");
    }

    public static Player GetPlayer1(ushort id)
    {
        Player player1 = new(id) { position = new Vector3(0.25f, 0.25f, 0.0f) };
        return player1;
    }

    public static Player GetPlayer2(ushort id)
    {
        Player player2 = new(id) { position = new Vector3(0.25f, 0.75f, 0.0f) };
        return player2;
    }

    public void SetReady()
    {
        IsReady = true;
    }

    public void SetDead()
    {
        Dead = true;
    }

    public void UpdatePosition(Vector3 position)
    {
        this.position = position;
    }

    #region MessagesFromPlayer
    public void SendPosition()
    {
        Message playerPosition = Message.Create(MessageSendMode.Unreliable, (ushort)(ServerToClientId.playerData));
        playerPosition.AddUShort(id);
        playerPosition.AddVector3(position);

        NetworkManager.Singleton.Server.SendToAll(playerPosition);
    }
    #endregion

    #region MessagesToPlayer
    public void RecvSession(ushort sessionId)
    {
        Message recvSession = Message.Create(MessageSendMode.Reliable, (ushort)(ServerToClientId.session));
        recvSession.AddUShort(sessionId);

        NetworkManager.Singleton.Server.Send(recvSession, id);
    }

    public void RecvJoinNotif(ushort playerId)
    {
        Message notifyPlayer1 = Message.Create(MessageSendMode.Reliable, (ushort)(ServerToClientId.playerJoined));
        notifyPlayer1.AddUShort(playerId);

        NetworkManager.Singleton.Server.Send(notifyPlayer1, id);
    }
    #endregion
}

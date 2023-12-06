using Riptide;
using UnityEngine;

public class Player
{
    public readonly ushort id;
    private Vector3 position;

    public Player(ushort id)
    {
        this.id = id;
        position = Vector3.zero;
    }

    public Player GetPlayer1(ushort id)
    {
        Player player1 = new(id) { position = new Vector3(0.25f, 0.25f, 0.0f) };
        return player1;
    }

    public Player GetPlayer2(ushort id)
    {
        Player player2 = new(id) { position = new Vector3(0.25f, 0.75f, 0.0f) };
        return player2;
    }

    public void UpdatePosition(Vector3 position)
    {
        this.position = position;
    }

    public void SendPosition()
    {
        Message playerPosition = Message.Create(MessageSendMode.Unreliable, (ushort)(ServerToClientId.playerData));
        playerPosition.AddUShort(id);
        playerPosition.AddVector3(position);

        NetworkManager.Singleton.Server.SendToAll(playerPosition);
    }
}

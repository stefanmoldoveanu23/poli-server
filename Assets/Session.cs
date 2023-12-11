using Riptide;
using UnityEngine;

public class Session
{
    public readonly ushort id;

    private Player player1;
    private Player player2;

    private ushort readyToRestart;

    public Session(ushort id)
    {
        this.id = id;
        player1 = player2 = null;
        readyToRestart = 0;

        Debug.Log($"(SESSION): Session started with id {this.id}.");
    }

    public void Update()
    {
        if (IsReady())
        {
            if (!player1.Dead)
            {
                player1.SendPosition();
            }

            if (!player2.Dead)
            {
                player2.SendPosition();
            }
        }
    }

    public void Reset()
    {
        player1 = Player.GetPlayer1(player1.id);
        player2 = Player.GetPlayer2(player2.id);

        readyToRestart = 0;
    }

    public void SetReady(ushort playerId)
    {
        if (player1.id == playerId)
        {
            player1.SetReady();
        }
        else
        {
            player2.SetReady();
        }
    }

    public bool IsReady()
    {
        return player1.IsReady && player2.IsReady;
    }

    public void HandleReadyToRestart(bool wantsRestart)
    {
        if (wantsRestart)
        {
            ++readyToRestart;
        }
        else
        {
            if (readyToRestart > 0)
            {
                --readyToRestart;
            }
        }

        ushort value = readyToRestart;

        if (readyToRestart == 2)
        {
            Reset();
            player1.SetReady();
            player2.SetReady();
        }

        SendUpdateRestartCount(value);
    }

    public void AddPlayer(ushort playerId)
    {
        if (player1 == null)
        {
            player1 = Player.GetPlayer1(playerId);
            player1.RecvSession(id);
        }
        else
        {
            player2 = Player.GetPlayer2(playerId);
            player2.RecvSession(id);

            player1.RecvJoinNotif(player2.id);
            player2.RecvJoinNotif(player1.id);
        }
    }

    public void UpdatePlayerPosition(ushort playerId, Vector3 position)
    {
        if (playerId == player1.id)
        {
            player1.UpdatePosition(position);
        }
        else
        {
            player2.UpdatePosition(position);
        }
    }

    public void HandlePlayerAction(ushort playerId, ushort action)
    {
        switch (action)
        {
            case (ushort)PlayerActions.gotHit:
                {
                    break;
                }
            case (ushort)PlayerActions.died:
                {
                    if (player1.id == playerId)
                    {
                        player1.SetDead();
                    }
                    else
                    {
                        player2.SetDead();
                    }

                    if (player1.Dead && player2.Dead)
                    {
                        SendGameOver();
                    }

                    break;
                }
            default:
                {
                    Debug.LogError($"(PLAYER): Error; No handler for action with id {action}.");
                    break;
                }
        }

        SendAction(playerId, action);
    }

    #region MessagesFromSession
    public void StartGame()
    {
        Message startGame = Message.Create(MessageSendMode.Reliable, (ushort)(ServerToClientId.startGame));
        startGame.AddUShort(id);

        NetworkManager.Singleton.Server.Send(startGame, player1.id);
        NetworkManager.Singleton.Server.Send(startGame, player2.id);
    }

    private void SendAction(ushort playerId, ushort action)
    {
        Message sendAction = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.playerAction);
        sendAction.AddUShort(playerId);
        sendAction.AddUShort(action);

        NetworkManager.Singleton.Server.Send(sendAction, player1.id);
        NetworkManager.Singleton.Server.Send(sendAction, player2.id);
    }

    private void SendUpdateRestartCount(ushort count)
    {
        Message updateRestartCount = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.updateRestartCount);
        updateRestartCount.AddUShort(count);

        NetworkManager.Singleton.Server.Send(updateRestartCount, player1.id);
        NetworkManager.Singleton.Server.Send(updateRestartCount, player2.id);
    }

    private void SendGameOver()
    {
        Message gameOver = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.gameOver);

        NetworkManager.Singleton.Server.Send(gameOver, player1.id);
        NetworkManager.Singleton.Server.Send(gameOver, player2.id);
    }
    #endregion
}

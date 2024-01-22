using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Riptide;
using UnityEditorInternal.Profiling.Memory.Experimental.FileFormat;
using System;

public class Tester
{

    [UnityTest]
    public IEnumerator TestConnection()
    {
        NetworkManager network = MonoBehaviour.Instantiate<NetworkManager>(Resources.Load<NetworkManager>("NetworkManager"));
        Assert.NotNull(network);

        yield return null;

        Assert.AreEqual(network.ClientCount(), 0);

        while (network.ClientCount() == 0)
        {
            yield return null;
        }

        Debug.Log("(SERVER): Client joined.");

        while (network.PlayerCount() == 0)
        {
            yield return null;
        }

        Message newEnemy = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.newEnemy);
        newEnemy.AddString(Guid.NewGuid().ToString());
        newEnemy.AddUShort(0);
        newEnemy.AddVector3(Vector3.zero);

        network.Server.Send(newEnemy, network.Server.Clients[0].Id);

        while (network.PlayerCount() != 0)
        {
            yield return null;
        }

        yield return null;
    }
}

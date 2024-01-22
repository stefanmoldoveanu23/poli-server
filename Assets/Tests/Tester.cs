using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class Tester
{
    [Test]
    public void TestEnemyHurt()
    {
        Enemy enemy = new();

        int hurtCount = Random.Range(0, 5);
        for (int i = 0; i < hurtCount; ++i)
        {
            enemy.GetHurt();
        }

        Assert.AreEqual(enemy.Health, 5 - hurtCount);
    }

    [Test]
    public void TestPlayerGetters()
    {
        Player player1 = Player.GetPlayer1(3);

        Assert.AreEqual(player1.id, 3);

        Player player2 = Player.GetPlayer2(7);

        Assert.AreEqual(player2.id, 7);
    }

    [Test]
    public void TestPlayerSetters()
    {
        Player player1 = Player.GetPlayer1(1);

        Assert.AreEqual(player1.Dead, false);
        Assert.AreEqual(player1.IsReady, false);

        player1.SetDead();
        Assert.AreEqual(player1.Dead, true);

        player1.SetReady();
        Assert.AreEqual(player1.IsReady, true);
    }
}

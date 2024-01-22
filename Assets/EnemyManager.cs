using Riptide;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public enum PowerUpType : ushort
{
    none = 0,
    doubleFire = 1,
    shield = 2,
}

public class EnemyManager
{
    private readonly Guid sessionId;
    public ushort NumberOfEnemyTypes { get; private set; }

    private readonly float spawnInterval;
    private readonly float speedUpdateInterval;

    private float timeForSpawn;
    private float timeForSpeedUpdate;

    private readonly Dictionary<Guid, Enemy> enemyList;

    public EnemyManager(Guid sessionId, ushort numberOfEnemyTypes, float spawnInterval, float speedUpdateInterval)
    {
        this.sessionId = sessionId;

        this.spawnInterval = spawnInterval;
        this.speedUpdateInterval = speedUpdateInterval;
        NumberOfEnemyTypes = numberOfEnemyTypes;

        timeForSpawn = spawnInterval;
        timeForSpeedUpdate = speedUpdateInterval;

        enemyList = new Dictionary<Guid, Enemy>();
    }

    public void Update()
    {
        timeForSpawn -= Time.deltaTime;
        if (timeForSpawn <= 0.0f)
        {
            timeForSpawn = spawnInterval;

            InstantiateEnemy();
        }

        timeForSpeedUpdate -= Time.deltaTime;
        if (timeForSpeedUpdate <= 0.0f)
        {
            timeForSpeedUpdate = speedUpdateInterval;

            UpdateEnemySpeed();
        }
    }

    // Instantiate an enemy at a random position on the y-axis with a speed and direction
    public void InstantiateEnemy()
    {
        Guid enemyGuid = Guid.NewGuid();
        ushort enemyType = (ushort)(Random.value * NumberOfEnemyTypes);
        Vector3 enemyPosition = new(18.0f, Random.Range(-9f, 6.5f), 0);

        Message newEnemy = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.newEnemy);
        newEnemy.AddString(enemyGuid.ToString());
        newEnemy.AddUShort(enemyType);
        newEnemy.AddVector3(enemyPosition);

        enemyList.Add(enemyGuid, new Enemy());
        NetworkManager.Singleton.SendToAllInSession(sessionId, newEnemy);
    }

    // Update the speed of all enemies
    public void UpdateEnemySpeed()
    {
        Message updateEnemySpeed = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.updateEnemySpeed);
        NetworkManager.Singleton.SendToAllInSession(sessionId, updateEnemySpeed);
    }

    public void HandleEnemyHurt(Guid guid)
    {
        if (enemyList.ContainsKey(guid))
        {
            enemyList[guid].GetHurt();
            if (enemyList[guid].Health <= 0.0f)
            {
                Message enemyDead = Message.Create(MessageSendMode.Reliable, (ushort)ServerToClientId.enemyDead);
                enemyDead.AddString(guid.ToString());

                if (Random.value > 0.9f)
                {
                    enemyDead.AddUShort((ushort)PowerUpType.doubleFire);
                    enemyDead.AddString(Guid.NewGuid().ToString());
                }
                else if (Random.value < 0.1f)
                {
                    enemyDead.AddUShort((ushort)PowerUpType.shield);
                    enemyDead.AddString(Guid.NewGuid().ToString());
                }
                else
                {
                    enemyDead.AddUShort((ushort)PowerUpType.none);
                }

                NetworkManager.Singleton.SendToAllInSession(sessionId, enemyDead);
                enemyList.Remove(guid);
            }
        }
    }

    public int EnemyCount()
    {
        return enemyList.Count;
    }
}

using System;

public class Enemy
{
    public float Health { get; private set; }

    public Enemy()
    {
        Health = 5.0f;
    }

    public void GetHurt()
    {
        Health -= 1.0f;
    }
}

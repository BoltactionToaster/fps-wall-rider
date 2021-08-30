using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class BaseState
{
    protected PlayerController player;

    //Currently no ExitState method, but it may be useful later

    public BaseState(PlayerController player)
    {
        this.player = player;
    }

    public abstract void EnterState();
    public abstract void Update();
}

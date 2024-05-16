using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public abstract class BaseState
{
    public abstract void StateEnter();
    public abstract void StateUpDate();
    public abstract void StateExit();
    public abstract void OnCollisionEnter(Collision collision);
}
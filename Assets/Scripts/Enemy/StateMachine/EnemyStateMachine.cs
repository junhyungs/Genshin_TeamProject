using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum EnemyState
{
    Idle,
    Move,
    Trace,
    TraceAttack,
    TraceMove
}

public class EnemyStateMachine : MonoBehaviour
{
    private Dictionary<EnemyState, BaseState> EnemyStateDic = new Dictionary<EnemyState, BaseState>();  
    BaseState State;

    private void Start()
    {
        InitState();
    }

    void Update()
    {
        State.StateUpDate();
    }

    public void InitState()
    {
        State = EnemyStateDic[EnemyState.Idle];
        State.StateEnter();
    }

    public void AddState(EnemyState addState, BaseState State)
    {
        EnemyStateDic.Add(addState, State);
    }

    public void ChangeState(EnemyState changeState)
    {
        State.StateExit();
        State = EnemyStateDic[changeState];
        State.StateEnter();
    }

    private void OnCollisionEnter(Collision collision)
    {
        State.OnCollisionEnter(collision);
    }
}

public enum BossState
{
    Idle,
    Move,
    Jump,
    Tail,
    Claw,
    Charge,
    Stamp
}

public class BossStateMachine : MonoBehaviour
{
    Dictionary<BossState, WolfState> BossStateDic = new Dictionary<BossState, WolfState>();
    WolfState State;

    private void Start()
    {
        InitState();
    }
    private void FixedUpdate()
    {
        State.StateFixedUpdate();
    }

    public void InitState()
    {
        State = BossStateDic[BossState.Move];
        State.StateEnter();
    }

    public void AddState(BossState state, WolfState baseState)
    {
        BossStateDic.Add(state, baseState);
    }

    public WolfState GetState(BossState state)
    {
        return BossStateDic[state];
    }

    public void ChangeState(BossState changeState)
    {
        State.StateExit();
        State = BossStateDic[changeState];
        State.StateEnter();
    }
}

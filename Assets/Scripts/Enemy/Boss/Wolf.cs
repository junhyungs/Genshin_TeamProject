using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.PlayerLoop;
using UnityEngine.UI;

public enum BossPattern
{
    JumpAttack,
    ClawAttack,
    ChargeAttack,
    StampAttack,
    DriftAttack,
    HowlAttack
}

public class Wolf : Enemy
{
    private bool battleStart = true;
    private bool turn = true;
    private bool moveStop = false;
    private bool jumpBack = true;
    private bool isJump = true;
    private bool isCharge = true;   
    private float paralyzation;
    private BossPattern Pattern;
    private Rigidbody bossRigid;
    
    //Pattern----------------------------------------------------------
    private IPattern bossAttack;
    
    private new void Awake()
    {
        InitWolf();
        InitState();
    }

    public void InitWolf()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        bossRigid = GetComponent<Rigidbody>();
        EnemyHealthDic = new Dictionary<Enemy, float>();
        Player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        enemyData = new EnemyData(200f, 50f, 4f, 0.3f, 9999, Element.Ice);
        EnemyHealthDic.Add(this, enemyData.Health);
        paralyzation = 100f;

        HpSlider = transform.GetComponentInChildren<Slider>();
        Hp = HpSlider.gameObject;
    }

    public void InitState()
    {
        bossState = gameObject.AddComponent<BossStateMachine>();
        bossState.AddState(BossState.Idle, new WolfIdle(this));
        bossState.AddState(BossState.Move, new WolfMove(this));
        bossState.AddState(BossState.Attack, new WolfAttackState(this));
        bossState.AddState(BossState.Claw, new WolfAttackState_Claw(this));
        bossState.AddState(BossState.Jump, new WolfAttackState_Jump(this));
        bossState.AddState(BossState.Charge, new WolfAttackState_Charge(this));
        bossState.AddState(BossState.Stamp, new WolfAttackState_Stamp(this));
        bossState.AddState(BossState.Drift, new WolfAttackState_Drift(this));
        bossState.AddState(BossState.Howl, new WolfAttackState_Howl(this));
    }

    private void Update()
    {
        Debug.Log(EnemyHealthDic[this]);
    }

    public void SetPattern(BossPattern bossPattern)
    {
        switch (bossPattern)
        {
            case BossPattern.JumpAttack:
                bossAttack = new JumpAttack(this);
                break;
            case BossPattern.ClawAttack:
                bossAttack = new ClawAttack(this);
                break;
            case BossPattern.ChargeAttack:
                bossAttack = new ChargeAttack(this);
                break;
            case BossPattern.StampAttack:
                bossAttack = new StampAttack(this);
                break;
            case BossPattern.DriftAttack:
                bossAttack = new DriftAttack(this);
                break;
            case BossPattern.HowlAttack:
                bossAttack = new HowlAttack(this);
                break;

        }
    }
    public IPattern Attack => bossAttack;
    public BossStateMachine State => bossState;
    public Animator BossAnimator => animator;
    public NavMeshAgent BossAgent => agent;
    public Transform PlayerTransform => Player;
    public Rigidbody BossRigid => bossRigid;
    public BossPattern BossPattern
    {
        get { return Pattern; }
        set { Pattern = value; }
    }
    public float Paralyzation
    {
        get { return paralyzation; }
        set { paralyzation = value; }
    }
   
    public bool Turn
    {
        get { return turn; }
        set { turn = value; }
    }
    public bool BattleStart
    {
        get { return battleStart; }
        set { battleStart = value; }
    }
    public bool MoveStop
    {
        get { return moveStop; }
        set { moveStop = value; }
    }
    public bool JumpBack
    {
        get { return jumpBack; }
        set { jumpBack = value; }
    }
    public bool IsJump
    {
        get { return isJump; }
        set { isJump = value; }
    }

    public bool IsCharge
    {
        get { return isCharge; }
        set { isCharge = value; }
    }

    public IEnumerator JumpCoolTime()
    {
        yield return new WaitForSeconds(7.5f);
        
        isJump = true;
    }

    public IEnumerator ChargeCoolTime()
    {
        yield return new WaitForSeconds(10f);
     
        isCharge = true;
    }

    public override void Splash(float damage)
    {
        
    }

    // Animation Event ----------------------------------------------
   
    public void OnTurn()
    {
        turn = true;
        moveStop = false;
    }
    public void Stop()
    {
        moveStop = true;
    }

    public void OnJumpBack()
    {
        jumpBack = true;
        moveStop = false;
    }
    public void ChangeAttack()
    {
        bossState.ChangeState(BossState.Attack);
    }

    public void Zero()
    {
        bossRigid.velocity = Vector3.zero;
    }

}

public abstract class WolfState : BossBaseState
{
    protected Wolf m_Wolf;
    public WolfState(Wolf wolf)
    {
        m_Wolf = wolf;
    }
}

public class WolfIdle : WolfState
{
    public WolfIdle(Wolf wolf) : base(wolf) { }

    private bool changeState;

    public override void StateEnter()
    {
        Init_IdleState();
    }

    public override void StateExit()
    {
        Exit_IdleState();
    }
    public override void StateFixedUpdate()
    {
        if (changeState)
        {
            m_Wolf.State.ChangeState(BossState.Attack);
        }
    }

    private void Init_IdleState()
    {
        m_Wolf.BossAnimator.SetBool("Idle", true);
        changeState = false;
        m_Wolf.StartCoroutine(Timer());
    }

    private void Exit_IdleState()
    {
        m_Wolf.BossAnimator.SetBool("Idle", false);
        m_Wolf.Paralyzation = 100f;
    }

    private IEnumerator Timer()
    {
        yield return new WaitForSeconds(4.0f);
        changeState = true;
    }
    
}

public class WolfMove : WolfState
{
    public WolfMove(Wolf wolf) : base(wolf) { }

    float timer = 0;

    public override void StateEnter()
    {
        Init_MoveState();
    }

    public override void StateExit()
    {
        Exit_MoveState();
    }

    public override void StateFixedUpdate()
    {
        if (m_Wolf.BattleStart)
        {
            AgentMove();
        }
    }

    private void Init_MoveState()
    {
        m_Wolf.BossAgent.enabled = true;
        m_Wolf.BossAgent.SetDestination(m_Wolf.PlayerTransform.position);
        m_Wolf.BossAnimator.applyRootMotion = false;
    }

    private void AgentMove()
    {
        timer += Time.fixedDeltaTime;

        if (Distance() > m_Wolf.BossAgent.stoppingDistance && timer <= 7.0f)
        {
            m_Wolf.BossAgent.SetDestination(m_Wolf.PlayerTransform.position);
        }
        else
            m_Wolf.State.ChangeState(BossState.Attack);
    }

    private void Exit_MoveState()
    {
        m_Wolf.BossAgent.SetDestination(m_Wolf.transform.position);
        m_Wolf.BossAgent.enabled = false;
        m_Wolf.BossAnimator.applyRootMotion = true;
    }
    private float Distance()
    {
        return Vector3.Distance(m_Wolf.transform.position, m_Wolf.PlayerTransform.position);
    }
}

public class WolfAttackState : WolfState
{
    public WolfAttackState(Wolf wolf) : base(wolf) { }

    private float angle;
    private float distance;

    public override void StateEnter()
    {
        Init_AttackState();
    }

    public override void StateExit()
    {
        Exit_AttackState();
    }

    public override void StateFixedUpdate()
    {
        angle = Angle();
        distance = Distance();
        
        Attack(angle, distance);
    }

    private void Init_AttackState()
    {
        if (m_Wolf.Paralyzation <= 0)
        {
            m_Wolf.JumpBack = true;
            m_Wolf.Turn = true;
            m_Wolf.State.ChangeState(BossState.Idle);
        }
    }

    private void Exit_AttackState()
    {
        angle = 0;
        distance = 0;
    }
    private void Attack(float Angle, float Distance)
    {
        JumpBack(Angle, Distance);
        Turn(Angle);

        if (!m_Wolf.MoveStop)
        {
            if (Distance <= 10.0f)
            {
                MeleeAttack(Angle);
            }
            else if (Distance <= 20.0f && m_Wolf.IsJump)
            {
                m_Wolf.IsJump = false;
                m_Wolf.StartCoroutine(m_Wolf.JumpCoolTime());
                m_Wolf.State.ChangeState(BossState.Jump);
            }
            else if (Distance <= 100f && m_Wolf.IsCharge)
            {
                m_Wolf.IsCharge = false;
                m_Wolf.StartCoroutine(m_Wolf.ChargeCoolTime());
                m_Wolf.State.ChangeState(BossState.Charge);
            }
            else if (!m_Wolf.IsJump && !m_Wolf.IsCharge)
            {
                m_Wolf.State.ChangeState(BossState.Howl);
            }
            else
            {
                m_Wolf.State.ChangeState(BossState.Move);
            }
                
        }
    }

    private void MeleeAttack(float Angle)
    {
        float Abs = Mathf.Abs(Angle);        

        if (Abs <= 60)
        {
            int random = Random.Range(0, 2);

            switch (random)
            {
                case 0:
                    m_Wolf.State.ChangeState(BossState.Claw);
                    break;
                case 1:
                    m_Wolf.State.ChangeState(BossState.Stamp);
                    break;
            }
        }
        else if (Abs <= 100)
        {
            m_Wolf.State.ChangeState(BossState.Drift);
        }
        else
            return;

    }

    private float Angle()
    {
        Vector3 targetDirection = (m_Wolf.PlayerTransform.position - m_Wolf.transform.position).normalized;

        Vector3 selfDirection = m_Wolf.transform.forward;

        float angle = Vector3.SignedAngle(selfDirection, targetDirection, Vector3.up);

        return angle;
    }

    private float Distance()
    {
        return Vector3.Distance(m_Wolf.transform.position, m_Wolf.PlayerTransform.position);
    }

    private void JumpBack(float Angle, float Distance)
    {
        if (!m_Wolf.Turn)
            return;
            
        if (Distance <= 6.0f && m_Wolf.JumpBack)
        {
            if (Angle > -90.0f && Angle < 90.0f)
            {
                m_Wolf.JumpBack = false;

                Vector3 targetDirection = m_Wolf.PlayerTransform.position - m_Wolf.transform.position;

                targetDirection.y = 0;

                float angle = Mathf.Atan2(targetDirection.x, targetDirection.z) * Mathf.Rad2Deg;

                m_Wolf.transform.rotation = Quaternion.Euler(0, angle, 0);

                m_Wolf.BossAnimator.SetTrigger("JumpBack");
            }

        }

    }

    private void Turn(float Angle)
    {
        if (!m_Wolf.JumpBack)
            return;
        
        if (Angle >= 90.0f && m_Wolf.Turn)
        {
            m_Wolf.BossAnimator.SetTrigger("TurnRight");
            m_Wolf.Turn = false;
        }
        else if (Angle <= -90.0f && m_Wolf.Turn)
        {
            m_Wolf.BossAnimator.SetTrigger("TurnLeft");
            m_Wolf.Turn = false;
        }
    }
 
}

public class WolfAttackState_Howl : WolfState
{
    public WolfAttackState_Howl(Wolf wolf) : base(wolf) { }
    
    public override void StateEnter()
    {
        m_Wolf.BossPattern = BossPattern.HowlAttack;
        m_Wolf.SetPattern(m_Wolf.BossPattern);
    }

    public override void StateExit()
    {
        
    }

    public override void StateFixedUpdate()
    {
        m_Wolf.Attack.BossAttack();
    }
}

public class WolfAttackState_Stamp : WolfState
{
    public WolfAttackState_Stamp(Wolf wolf) : base(wolf) { }
    
    public override void StateEnter()
    {
        m_Wolf.BossPattern = BossPattern.StampAttack;
        m_Wolf.SetPattern(m_Wolf.BossPattern);
    }

    public override void StateExit()
    {
        
    }

    public override void StateFixedUpdate()
    {
        m_Wolf.Attack.BossAttack();
    }
}

public class WolfAttackState_Jump : WolfState
{
    public WolfAttackState_Jump(Wolf wolf) : base(wolf) { }

    public override void StateEnter()
    {
        m_Wolf.BossRigid.isKinematic = true;

        m_Wolf.BossPattern = BossPattern.JumpAttack;
        m_Wolf.SetPattern(m_Wolf.BossPattern);
    }

    public override void StateExit()
    {
        m_Wolf.BossRigid.isKinematic = false;
    }

    public override void StateFixedUpdate()
    {
        m_Wolf.Attack.BossAttack();
    }

    
}


public class WolfAttackState_Claw : WolfState
{
    public WolfAttackState_Claw(Wolf wolf) : base(wolf) { }
    
    public override void StateEnter()
    {
        m_Wolf.BossPattern = BossPattern.ClawAttack;
        m_Wolf.SetPattern(m_Wolf.BossPattern);

        m_Wolf.Attack.BossAttack();
    }
    public override void StateExit() { }
    public override void StateFixedUpdate() { }
}

public class WolfAttackState_Drift : WolfState
{
    public WolfAttackState_Drift(Wolf wolf) : base(wolf) { }
    
    public override void StateEnter()
    {
        m_Wolf.BossPattern = BossPattern.DriftAttack;
        m_Wolf.SetPattern(m_Wolf.BossPattern);

        m_Wolf.Attack.BossAttack();
    }
    public override void StateExit() { }
    public override void StateFixedUpdate() { }
}

public class WolfAttackState_Charge : WolfState
{
    public WolfAttackState_Charge(Wolf wolf) : base(wolf) { }

    public override void StateEnter()
    {
        m_Wolf.BossPattern = BossPattern.ChargeAttack;
        m_Wolf.SetPattern(m_Wolf.BossPattern);
    }

    public override void StateExit()
    {

    }

    public override void StateFixedUpdate()
    {
        m_Wolf.Attack.BossAttack();
    }
}


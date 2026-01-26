using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;

public abstract class BossCore : MonoBehaviour
{
    //--------[ 공개 변수 ]----------------------------------------------------------//
    [Header("상태")]
    [SerializeField] protected string currentPhase = "normal";
    [SerializeField] protected string currentState = "idle";
    [SerializeField] protected Coroutine motionCo = null;

    [Header("체력")]
    public int leg_L_MaxHP = 500;
    public int leg_L_CurrentHP;
    public int leg_R_MaxHP = 500;
    public int leg_R_CurrentHP;
    public int body_MaxHP = 500;
    public int body_CurrentHP;

    [Header("감지")]
    public Collider2D detectArea1;
    public Collider2D detectArea2;
    public Collider2D detectArea3;
    public Collider2D detectArea4;


    [Header("공격")]
    public float afterDelay;
    public float startAfterDelay;
    public Collider2D attackBox1;
    public Collider2D attackBox2;
    public Collider2D attackBox3;
    public Collider2D attackBox4;

    [Header("SFX")]
    public AudioSource audioSource;
    public AudioClip Sfx;

    [Header("공격 타겟 필터")]
    public string attackTag = "PlayerHurtPoint";
    public LayerMask attackLayers = ~0;
    public Collider2D playerHitBox;

    [Header("피격")]
    public float perAttackerCooldown = 0.0f;
    public bool hurtStateSkip = false;
    public GameObject[] hurtGoL;
    public GameObject[] hurtGoR;
    public GameObject[] hurtGoM;
    public Collider2D[] hurtColL;
    public Collider2D[] hurtColR;
    public Collider2D[] hurtColM;

    [Header("피격 타겟 필터(플레이어 공격 수신)")]
    public string hurtboxAcceptTag = "PlayerAttack";
    public LayerMask hurtboxAcceptLayers = ~0;

    [Header("Camera Shake (optional)")]
    public CameraShaker cameraShaker;

    [Header("씬 설정")]
    public Transform player;
    public HitImpactManager fx;

    [Header("데스 이펙트")]
    public GameObject deathEffect;
    public float deathAnimationLength = 10f;

    //--------[ 내부 ]---------------------------------------------------------------//
    protected Animator animator;
    protected SpriteRenderer[] sr;
    protected BossActFunc act;

    public HitFlash[] hfL;
    public HitFlash[] hfR;
    public HitFlash[] hfM;

    protected string _lastState = "";
    protected string _lastPhase = "";
    protected bool invincibility = false;

    protected bool x = true;

    //--------[ Awake ]--------------------------------------------------------------//
    protected virtual void Awake()
    {
        currentState = "idle(startDelay)";
        leg_L_CurrentHP = leg_L_MaxHP;
        leg_R_CurrentHP = leg_R_MaxHP;
        body_CurrentHP = body_MaxHP;

        animator = GetComponent<Animator>();
        act = GetComponent<BossActFunc>();

        detectArea1.enabled = false;
        detectArea2.enabled = false;
        detectArea3.enabled = false;
        detectArea4.enabled = false;

        if (!player)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("PlayerHurtPoint");
            if (!pObj) pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj) player = pObj.transform;
        }

        GameObject pCol = GameObject.FindGameObjectWithTag("Player");
        playerHitBox = pCol.GetComponent<Collider2D>();

        if (cameraShaker == null && Camera.main != null)
            cameraShaker = Camera.main.GetComponent<CameraShaker>();
    }

    //--------[ Enable/Disable ]-----------------------------------------------------//
    protected virtual void OnEnable()
    {
        
    }

    protected void Start()
    {
        if (attackBox1) attackBox1.enabled = false;
        if (attackBox2) attackBox2.enabled = false;
        if (attackBox3) attackBox3.enabled = false;
        if (attackBox4) attackBox4.enabled = false;

        hfL = new HitFlash[hurtGoL.Length];
        hfR = new HitFlash[hurtGoR.Length];
        hfM = new HitFlash[hurtGoM.Length];
        for (int i = 0; i < hurtGoL.Length; i++)
        {
            hfL[i] = hurtGoL[i].GetComponent<HitFlash>();
        }
        for (int i = 0; i < hurtGoR.Length; i++)
        {
            hfR[i] = hurtGoR[i].GetComponent<HitFlash>();
        }
        for (int i = 0; i < hurtGoM.Length; i++)
        {
            hfM[i] = hurtGoM[i].GetComponent<HitFlash>();
        }

        motionCo = StartCoroutine(act.DelayCo());

        for (int i = 0; i < hurtColL.Length; i++)
        {
            hurtColL[i].enabled = true;
        }
        for (int i = 0; i < hurtColR.Length; i++)
        {
            hurtColR[i].enabled = true;
        }
        for (int i = 0; i < hurtColM.Length; i++)
        {
            hurtColM[i].enabled = false;
        }
    }

    //--------[ UPDATE ]-------------------------------------------------------------//
    protected virtual void Update()
    {

        bool stateChanged = _lastState != currentState;
        bool phaseChanged = _lastPhase != currentPhase;

        if (phaseChanged)
        {
            if (currentPhase == "broken_L_Leg")
            {
                for (int i = 0; i < hurtColL.Length; i++)
                {
                    hurtColL[i].enabled = false;
                }
                for (int i = 0; i < hurtColR.Length; i++)
                {
                    hurtColR[i].enabled = true;
                }
                for (int i = 0; i < hurtColM.Length; i++)
                {
                    hurtColM[i].enabled = false;
                }
            }
            if (currentPhase == "broken_R_Leg")
            {
                for (int i = 0; i < hurtColL.Length; i++)
                {
                    hurtColL[i].enabled = true;
                }
                for (int i = 0; i < hurtColR.Length; i++)
                {
                    hurtColR[i].enabled = false;
                }
                for (int i = 0; i < hurtColM.Length; i++)
                {
                    hurtColM[i].enabled = false;
                }
            }
            if (currentPhase == "noLeg")
            {
                for (int i = 0; i < hurtColL.Length; i++)
                {
                    hurtColL[i].enabled = false;
                }
                for (int i = 0; i < hurtColR.Length; i++)
                {
                    hurtColR[i].enabled = false;
                }
                for (int i = 0; i < hurtColM.Length; i++)
                {
                    hurtColM[i].enabled = true;
                }
            }
        }

        if (currentState == "idle")
        {
            act.Detect();
            x = true;
        }

        if (x) // 원랜 stateChanged
        {
            
            if (currentState == "attack1_L") { motionCo = StartCoroutine(act.Attack1_LCo()); x = false; }
            if (currentState == "attack1_R") { motionCo = StartCoroutine(act.Attack1_RCo()); x = false; }
            if (currentState == "attack2_L") { motionCo = StartCoroutine(act.Attack2_LCo()); x = false; }
            if (currentState == "attack2_R") { motionCo = StartCoroutine(act.Attack2_RCo()); x = false; }
            if (currentState == "sumon") { motionCo = StartCoroutine(act.SumonCo()); x = false; }
            if (currentState == "gas") { motionCo = StartCoroutine(act.GasCo()); x = false; }
            if (currentState == "acid") { motionCo = StartCoroutine(act.AcidCo()); x = false; }
        }

        if (stateChanged) 
        {
            if (currentState == "broken_L") { motionCo = StartCoroutine(act.Broken_LCo()); }
            if (currentState == "broken_R") { motionCo = StartCoroutine(act.Broken_RCo()); }
            if (currentState == "die")         { act.Die(); }
            if (currentState == "idle(delay)") { motionCo = StartCoroutine(act.DelayCo()); }
            if (currentState == "idle(startDelay)") { motionCo = StartCoroutine(act.StartDelayCo()); }
        }

        _lastState = currentState;
        _lastPhase = currentPhase;
    }

}
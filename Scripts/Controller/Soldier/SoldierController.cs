using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

namespace WarGame
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SoldierData))]
    [RequireComponent(typeof(NavMeshAgent))]
    public class SoldierController : MonoBehaviour
    {
        [Header("Audio Clips")]
        public AudioClip gunFireSound;
        public AudioClip deathSound;

        [Header("Destinations")]
        public Transform startPoint;
        public List<Transform> destinations;
        public float occupiedDistance = 8f;
        public Transform destination;

        [Header("Others")]
        public LayerMask FireCollisionLayer;

        [HideInInspector] public List<GameObject> seenObjects;
        Vector3 offset = Vector3.zero;

        public SoldierData data;
        public SoldierHealth health;
        public RaderSystem rader;
        StateMachine<SoldierController> soldierSM;
        Dictionary<SoldierState, IState<SoldierController>> soldierStates = new Dictionary<SoldierState, IState<SoldierController>>();
        [HideInInspector] public Animator animator;
        [HideInInspector] public CharacterController characterController;
        [HideInInspector] public NavMeshAgent agent;

        bool isClearingTargetObj;
        bool drawRays;
        bool drawVisionCone;
        bool drawOverlapSphere;
        string hands = string.Empty;

        public enum SoldierState
        {
            Idle,
            Track,
            Move,
            Occupied,
            Attack,
            Dead
        }

        public bool IsEnemy { get { return data.IsEnemy; } }
        public float ShootDistance { get { return data.sightDistance * 0.5f; } }
        public bool IsOccupied
        {
            get
            {
                float currentDistance = (destination.transform.position - this.transform.position).sqrMagnitude;
                if (currentDistance <= occupiedDistance)
                    return true;
                else
                    return false;
            }
        }
        int OverlapSphereLayer
        {
            get
            {
                if (data.IsEnemy)
                {
                    return 1 << LayerMask.NameToLayer("Player") | 1 << LayerMask.NameToLayer("Ally");
                }
                else if (!data.IsEnemy)
                {
                    return 1 << LayerMask.NameToLayer("Enemy");
                }
                else
                    return 0;
            }
        }

        private void Awake()
        {
            GetComponents();
            SetPlayerStates();
        }

        void Start()
        {
            SetNaviAgent(); // default
            destination = destinations[0];
        }

        void Update()
        {
            //SearchTargetObject();
            soldierSM.OperateUpdate();
        }

        private void FixedUpdate()
        {
            soldierSM.OperateFixedUpdate();
        }

        bool IsValidArea(List<float> distances)
        {
            foreach (var dis in distances)
            {
                if (dis <= data.sightDistance)
                    return true;
            }
            return false;
        }

        List<float> GetAllTargetsDistance()
        {
            List<float> distances = new List<float>();
            foreach (var target in data.distanceToTargets)
            {
                distances.Add(target.Value);
            }

            return distances;
        }

        public bool IsValidTarget(GameObject target)
        {
            if(gameObject.CompareTag("Enemy"))
            {
                if (target.CompareTag("Ally") || target.CompareTag("Player")) return true;
            }
            else if(gameObject.CompareTag("Ally"))
            {
                if (target.CompareTag("Enemy")) return true;
            }
            return false;
        }

        void GetComponents()
        {
            animator = GetComponent<Animator>();
            characterController = GetComponent<CharacterController>();
            data = GetComponent<SoldierData>();
            health = GetComponent<SoldierHealth>();
            agent = GetComponent<NavMeshAgent>();
            rader = GetComponentInChildren<RaderSystem>();
        }

        public void SetNaviAgent(float movementSpeed = 2f, bool autoBreaking = false, float angularSpeed = 360f, float stoppingDistance = 1f)
        {
            agent.angularSpeed = angularSpeed;
            agent.speed = movementSpeed;
            agent.autoBraking = autoBreaking;
            agent.stoppingDistance = stoppingDistance;
        }

        public void TakeDamageAnim()
        {
            if (!health.Invulnerable && health.IsTakeDamage)
                animator.SetTrigger("Pain");
        }



        #region FSM
        void SetPlayerStates()
        {
            IState<SoldierController> idle = new SoldierIdle();
            IState<SoldierController> track = new SoldierTrack();
            IState<SoldierController> move = new SoldierMove();
            IState<SoldierController> occupied = new SoldierOccupied();
            IState<SoldierController> attack = new SoldierAttack();
            IState<SoldierController> dead = new SoldierDead();

            soldierStates.Add(SoldierState.Idle, idle);
            soldierStates.Add(SoldierState.Track, track);
            soldierStates.Add(SoldierState.Move, move);
            soldierStates.Add(SoldierState.Occupied, occupied);
            soldierStates.Add(SoldierState.Attack, attack);
            soldierStates.Add(SoldierState.Dead, dead);

            if (soldierSM == null)
            {
                soldierSM = new StateMachine<SoldierController>(this, soldierStates[SoldierState.Idle]);
            }
        }

        public void ChangeState(SoldierState state)
        {
            soldierSM.SetState(soldierStates[state]);
        }
        #endregion


    }
}
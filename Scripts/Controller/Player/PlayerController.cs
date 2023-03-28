﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace WarGame
{
    [ExecuteInEditMode]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(CapsuleCollider))]
    public class PlayerController : MonoBehaviour
    {
        [System.Serializable]
        public class PlayerStandState
        {
            public float ControlCameraHeight;
            public float ColliderHeight;
            public float ColliderCenterHeight;
        }

        #region Variables

        [Header("Helper")]
        public MouseControl mouseControl;
        public HeadBob headBob;        
        public LayerMask groundLayer;
        public MeshRenderer nearBlurSphere;

        [Header("Animation Curve")]
        public AnimationCurve HeadBobBlendCurve;
        public AnimationCurve HeadBobPeriodBlendCurve;
        public AnimationCurve RunIncreaseSpeedCurve;
        public AnimationCurve StateChangeCurve;

        [Header("State")]
        public PlayerStandState StandState;
        public PlayerStandState CrouchState;
        float stateChangeSpeed = 3f;
        [HideInInspector] public float weightSmooth = 6f;

        [Header("Movement")]
        public float speed = 2f;
        public float maxSpeed = 1f;
        public float jumpSpeed = 4f;
        float crouchSpeedMultiplier = 0.75f;
        float runSpeedMultiplier = 2f;
        float runIncreaseSpeedTime = 1f;
        float runSpeedThreshold = 1f;
        float gravity = 9.81f;
        float stickToGround = 9.81f;

        [Header("Others")]        
        public Transform directionRefence;        
        public Camera playerCamera;
        public Transform controlCamera;
        public Transform handsHeadBobTarget;
        public TransformNoise cameraNoise;
        
        float idleNoise = 0.5f;
        float runNoise = 4f;
        float cameraHeadbobWeight = 1f;
        float handsHeadbobWeight = 0.3f;
        [HideInInspector] public float handsHeadbobMultiplier = 1f;
        Vector3 playerVelocity = Vector3.zero;
        Vector3 oldPlayerVelocity = Vector3.zero;
        Vector3 oldPosition;
        Vector3 oldHandHeadBobPos;
        Vector3 oldCameraHeadBobPos;
        Vector3 controlCameraPosition;
        float standStateBlend;
        float runTime = 0f;
        float defaultHandsHeadbobWeight;

        public PlayerHands Hands;
        public PostProcessingController PPController;
        public PostProcessingController.DepthOfFieldSettings DefaultDofSettings;
        public PlayerFreezeChangedEvent PlayerFreezeChanged = new PlayerFreezeChangedEvent();
        CapsuleCollider charactarCollider;
        CharacterController controller;
        PlayerDamageHandler damageHandler;

        //FSM
        StateMachine<PlayerController> playerSM;
        Dictionary<PlayerState, IState<PlayerController>> playerStates 
            = new Dictionary<PlayerState, IState<PlayerController>>();

        bool isFreeze = false;
        bool isRunning;
        bool isOldGrounded = false;
        bool isCrouching;

        public enum PlayerState
        {
            Idle, 
            Move, 
            Crouch, 
            Jump
        }
        
        #endregion

        #region Params

        string ForwardAxisParam = "Vertical";
        string StrafeAxisParam = "Horizontal";
        string HandsParam = "Hands";
        string NeckParam = "Neck";
        
        #endregion

        #region Events
                
        [HideInInspector] public UnityAction RunStartEvent;
        [HideInInspector] public UnityAction JumpStartEvent;
        [HideInInspector] public UnityAction JumpFallEvent;
        [HideInInspector] public UnityAction JumpEndEvent;
        [HideInInspector] public UnityAction CrouchEvent;
        [HideInInspector] public UnityAction StandUpEvent;

        #endregion

        #region Properties

        public Vector3 PlayerVelocity { get { return controller.velocity; } }

        public bool IsFreezed { get { return isFreeze; } }

        public bool IsRunning { get { return isRunning; } }

        public bool IsCrouching { get { return isCrouching; } }

        public bool IsGrounded { get { return controller.isGrounded; } }

        public float DefaultHandsHeadbobWeight { get { return defaultHandsHeadbobWeight; } }

        public PlayerDamageHandler DamageHandler
        {
            get
            {
                if (damageHandler == null)
                    damageHandler = GetComponent<PlayerDamageHandler>();

                return damageHandler;
            }
        }

        #endregion

        #region FSM
        void SetPlayerStates()
        {
            IState<PlayerController> idle = new PlayerIdle();
            IState<PlayerController> move = new PlayerMove();
            IState<PlayerController> crouch = new PlayerCrouch();
            IState<PlayerController> jump = new PlayerJump();

            playerStates.Add(PlayerState.Idle, idle);
            playerStates.Add(PlayerState.Move, move);
            playerStates.Add(PlayerState.Crouch, crouch);
            playerStates.Add(PlayerState.Jump, jump);
        }

        void InitPlayerStateMachine()
        {
            if (playerSM == null)
            {
                playerSM = new StateMachine<PlayerController>(this, playerStates[PlayerState.Idle]);
            }
        }

        public void ChangeState(PlayerState state)
        {
            playerSM.SetState(playerStates[state]);
        }

        #endregion

        private void Awake()
        {
            GetComponents();
        }

        void Start()
        {
            mouseControl.Init(transform, controlCamera);
            InitHeadBobSystem();            
        }

        void Update()
        {
            LockMouseCursor();
        }

        void FixedUpdate()
        {
            mouseControl.LookRotation(Time.fixedDeltaTime);

            if (controller.isGrounded)
            {
                Move();
            }
            else
            {
                ApplyGravity();
            }                      

            JumpFallControl();
            JumpEndControl();

            UpdateCurrentPhysics();
        }

        void GetComponents()
        {
            damageHandler = GetComponent<PlayerDamageHandler>();
            charactarCollider = GetComponent<CapsuleCollider>();
            controller = GetComponent<CharacterController>();
            directionRefence = GameObject.FindGameObjectWithTag(NeckParam).transform;
            controlCamera = GameObject.FindGameObjectWithTag(NeckParam).transform;
            handsHeadBobTarget = GameObject.FindGameObjectWithTag(HandsParam).transform;
            playerCamera = Camera.main;
            cameraNoise = Camera.main.GetComponent<TransformNoise>();
        }

        void LockMouseCursor()
        {
            if (Input.GetMouseButtonDown(2) || Input.GetMouseButtonDown(1))
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        void InitHeadBobSystem()
        {
            defaultHandsHeadbobWeight = handsHeadbobWeight;
            controlCameraPosition = controlCamera.localPosition;
        }

        void UpdateCurrentPhysics()
        {
            oldPlayerVelocity = playerVelocity;
            isOldGrounded = controller.isGrounded;
            oldPosition = transform.position;
        }

        public void UpdateDefaultDeath()
        {
            mouseControl.RotateCameraSmoothlyTo(0, Time.deltaTime);
        }

        public void JumpEndControl()
        {
            if (controller.isGrounded && !isOldGrounded)
            {
                if (JumpEndEvent != null)
                {
                    JumpEndEvent();
                }
            }
        }

        public void JumpFallControl()
        {
            if (oldPlayerVelocity.y > 0 && playerVelocity.y < 0)
            {
                if (JumpFallEvent != null)
                {
                    JumpFallEvent();
                }
            }
        }

        public void Freeze(bool value)
        {
            if (!value && !damageHandler.Health.RealIsAlive)
                return;

            mouseControl.Enabled = !value;
            isFreeze = value;

            PlayerFreezeChanged.Invoke(isFreeze);
        }

        public void SetNoiseEnabled(bool isEnabled)
        {
            cameraNoise.enabled = isEnabled;
        }

        void Move()
        {
            float h = Input.GetAxis(StrafeAxisParam);
            float v = Input.GetAxis(ForwardAxisParam);

            if (isFreeze)
            {
                h = 0;
                v = 0;
            }

            headBob.CalcHeadbob(Time.time);

            handsHeadBobTarget.localPosition -= oldHandHeadBobPos;

            // you can do any HandsHeadBobTarget position set

            handsHeadBobTarget.localPosition += headBob.HeadBobPos * handsHeadbobWeight * handsHeadbobMultiplier;

            controlCamera.localPosition -= oldCameraHeadBobPos;

            controlCamera.localPosition = controlCameraPosition;
            // you can do any ControlCamera position set

            controlCamera.localPosition += headBob.HeadBobPos * cameraHeadbobWeight;

            oldHandHeadBobPos = headBob.HeadBobPos * handsHeadbobWeight * handsHeadbobMultiplier;
            oldCameraHeadBobPos = headBob.HeadBobPos * cameraHeadbobWeight;

            Vector3 moveVector = directionRefence.forward * v + directionRefence.right * h;
            Vector3 playerXZVelocity = Vector3.Scale(playerVelocity, new Vector3(1, 0, 1));

            float speed = this.speed;
            if (Input.GetKey(KeyCode.LeftShift) && !Input.GetMouseButton(1) && !Input.GetMouseButton(0) && playerXZVelocity.magnitude >= runSpeedThreshold && !isCrouching)
            {
                //speed *= RunSpeedMultiplier;
                runTime += Time.fixedDeltaTime;
                if (!isRunning)
                {
                    isRunning = true;
                    if (RunStartEvent != null)
                    {
                        RunStartEvent();
                    }
                }

                if (!Hands.IsAiming)
                    cameraNoise.NoiseAmount = Mathf.MoveTowards(cameraNoise.NoiseAmount, runNoise, Time.fixedDeltaTime * 5f);
            }
            else
            {
                runTime -= Time.fixedDeltaTime;

                if (!Hands.IsAiming)
                    cameraNoise.NoiseAmount = Mathf.MoveTowards(cameraNoise.NoiseAmount, idleNoise, Time.fixedDeltaTime * 5f);
                isRunning = false;
            }

            if (Input.GetKeyDown(KeyCode.LeftControl) && !isFreeze)
            {
                isCrouching = true;

                if (CrouchEvent != null)
                {
                    CrouchEvent();
                }
            }

            if (isCrouching)
            {

            }

            if ((Input.GetKeyUp(KeyCode.LeftControl) && !isFreeze) || (Input.GetKey(KeyCode.LeftControl) && isFreeze && isCrouching))
            {
                isCrouching = false;

                if (damageHandler.Health.RealIsAlive)
                {
                    if (StandUpEvent != null)
                    {
                        StandUpEvent();
                    }
                }

            }

            standStateChange();

            runTime = Mathf.Clamp(runTime, 0, runIncreaseSpeedTime);

            float runTimeFraction = runTime / runIncreaseSpeedTime;
            Hands.SetRun(runTimeFraction);
            float runMultiplier = Mathf.Lerp(1, runSpeedMultiplier, RunIncreaseSpeedCurve.Evaluate(runTimeFraction));
            speed *= runMultiplier;
            if (isCrouching)
                speed *= crouchSpeedMultiplier;

            Ray r = new Ray(transform.position, Vector3.down);
            RaycastHit hitInfo;

            Physics.SphereCast(r, charactarCollider.radius, out hitInfo, charactarCollider.height / 2f, groundLayer);

            Vector3 desiredVelocity = Vector3.ProjectOnPlane(moveVector, hitInfo.normal) * speed;
            playerVelocity.x = desiredVelocity.x;
            playerVelocity.z = desiredVelocity.z;
            playerVelocity.y = -stickToGround;

            Vector3 calculatedVelocity = playerVelocity;
            calculatedVelocity.y = 0;

            float speedFraction = calculatedVelocity.magnitude / maxSpeed;
            headBob.HeadBobWeight = Mathf.Lerp(headBob.HeadBobWeight, HeadBobBlendCurve.Evaluate(speedFraction), weightSmooth * Time.fixedDeltaTime);
            headBob.HeadBobPeriod = HeadBobPeriodBlendCurve.Evaluate(speedFraction);

            if (controller.isGrounded)
            {
                if (Input.GetKey(KeyCode.Space) && !isCrouching && !isFreeze)
                {
                    playerVelocity.y = jumpSpeed;
                    if (JumpStartEvent != null)
                        JumpStartEvent();
                }
                controller.Move(playerVelocity * Time.fixedDeltaTime);
            }
        }

        void standStateChange()
        {
            standStateBlend = Mathf.MoveTowards(standStateBlend, isCrouching ? 1f : 0f, Time.deltaTime * stateChangeSpeed);

            charactarCollider.height = Mathf.Lerp(
                StandState.ColliderHeight,
                CrouchState.ColliderHeight,
                StateChangeCurve.Evaluate(standStateBlend)
                );


            Vector3 colliderCenter = charactarCollider.center;

            colliderCenter.y = Mathf.Lerp(
                StandState.ColliderCenterHeight,
                CrouchState.ColliderCenterHeight,
                StateChangeCurve.Evaluate(standStateBlend)
                );
            charactarCollider.center = colliderCenter;

            controller.height = charactarCollider.height;
            controller.center = charactarCollider.center;

            controlCameraPosition.y = Mathf.Lerp(
                StandState.ControlCameraHeight,
                CrouchState.ControlCameraHeight,
                StateChangeCurve.Evaluate(standStateBlend)
                );
        }

        public void SetSensivityMultiplier(float multiplier)
        {
            mouseControl.SensivityMultiplier = multiplier;
        }

        void ApplyGravity()
        {
            playerVelocity += Vector3.down * gravity * Time.fixedDeltaTime;
            controller.Move(playerVelocity * Time.fixedDeltaTime);
        }

        [System.Serializable]
        public class HeadBob
        {
            public bool Enabled = true;
            public float HeadBobWeight = 1f;
            public Vector2 HeadBobAmount = new Vector2(0.11f, 0.08f);
            public float HeadBobPeriod = 1f;
            public AnimationCurve HeadBobCurveX;
            public AnimationCurve HeadBobCurveY;

            public Vector3 HeadBobPos
            {
                get
                {
                    return resultHeadbob;
                }
            }

            Vector3 resultHeadbob;

            public void CalcHeadbob(float currentTime)
            {
                float headBob = Mathf.PingPong(currentTime, HeadBobPeriod) / HeadBobPeriod;

                Vector3 headBobVector = new Vector3();

                headBobVector.x = HeadBobCurveX.Evaluate(headBob) * HeadBobAmount.x;
                headBobVector.y = HeadBobCurveY.Evaluate(headBob) * HeadBobAmount.y;

                headBobVector = Vector3.LerpUnclamped(Vector3.zero, headBobVector, HeadBobWeight);

                if (!Application.isPlaying)
                {
                    headBobVector = Vector2.zero;
                }

                if (Enabled)
                {
                    resultHeadbob = headBobVector;
                }
            }
        }

        [System.Serializable]
        public class MouseControl
        {
            public bool Enabled;
            public float XSensitivity = 2f;
            public float YSensitivity = 2f;
            public float SensivityMultiplier = 1f;
            public float MinimumX = -90F;
            public float MaximumX = 90F;
            public float SmoothTime = 15f;
            public bool ClampVerticalRotation = true;

            public string AxisXName = "Mouse X";
            public string AxisYName = "Mouse Y";

            private Quaternion characterTargetRot;
            private Quaternion cameraTargetRot;

            private Transform character;
            private Transform camera;

            public void Init(Transform character, Transform camera)
            {
                characterTargetRot = character.localRotation;
                cameraTargetRot = camera.localRotation;

                this.character = character;
                this.camera = camera;
            }

            public void LookRotation(float deltaTime)
            {
                if (!Enabled)
                    return;

                LookRotation(Input.GetAxis(AxisXName) * XSensitivity * SensivityMultiplier, Input.GetAxis(AxisYName) * YSensitivity * SensivityMultiplier, deltaTime);
            }

            public void LookRotation(float yRot, float xRot, float deltaTime)
            {
                characterTargetRot *= Quaternion.Euler(0f, yRot, 0f);
                cameraTargetRot *= Quaternion.Euler(-xRot, 0f, 0f);

                if (ClampVerticalRotation)
                    cameraTargetRot = ClampRotationAroundXAxis(cameraTargetRot);

                character.localRotation = Quaternion.Slerp(character.localRotation, characterTargetRot, SmoothTime * deltaTime);
                camera.localRotation = Quaternion.Slerp(camera.localRotation, cameraTargetRot, SmoothTime * deltaTime);
            }

            public void RotateCameraSmoothlyTo(float xRot, float deltaTime)
            {
                cameraTargetRot = Quaternion.Euler(xRot, 0f, 0f);

                if (ClampVerticalRotation)
                    cameraTargetRot = ClampRotationAroundXAxis(cameraTargetRot);

                camera.localRotation = Quaternion.Slerp(camera.localRotation, cameraTargetRot, SmoothTime * deltaTime);
            }

            Quaternion ClampRotationAroundXAxis(Quaternion q)
            {
                q.x /= q.w;
                q.y /= q.w;
                q.z /= q.w;
                q.w = 1.0f;

                float angleX = 2.0f * Mathf.Rad2Deg * Mathf.Atan(q.x);

                angleX = Mathf.Clamp(angleX, MinimumX, MaximumX);

                q.x = Mathf.Tan(0.5f * Mathf.Deg2Rad * angleX);

                return q;
            }

        }
    }

    public class PlayerFreezeChangedEvent : UnityEvent<bool>
    { }
}
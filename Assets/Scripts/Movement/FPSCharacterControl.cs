using Eclipse.Gameplay;
using QFSW.QC;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Eclipse.Character {
    public class FPSCharacterControl : NetworkBehaviour
    {
        [SerializeField] Input.PlayerInputManager pim;
        
        [Command, Tooltip("How many times to raycast against the ground to determine the normal. Minimum recommended is 4, always raycasts one additional time.")] public float groundNormalIterations;
        [Command, SerializeField] float jumpForce = 10f;
        public NetworkVariable<float> maxMoveSpeed = new NetworkVariable<float>(5, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> moveSpeed = new NetworkVariable<float>(5, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> airAcceleration = new NetworkVariable<float>(5, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        public NetworkVariable<float> gravity = new(9.81f);
        public NetworkVariable<float> velocityDamping = new(1);
        public NetworkVariable<float> groundedFriction = new(1);
        [SerializeField] Transform groundCheckOrigin;
        public LayerMask terrainMask;
        [SerializeField] Rigidbody rb;
        [Command]
        public float lookSpeed = 15f;
        [Command]
        public bool invertHeadRotation;
        [Command] public ForceMode moveForceMode;
        [SerializeField] float groundCheckRadius;
        public (Quaternion bodyRot, Quaternion localHead) CharacterRotation()
        {
            return (transform.rotation, head.localRotation);
        }
        [SerializeField] Transform head;
        [SerializeField] protected float headAngle;
        [SerializeField] bool grounded;
        [SerializeField] bool groundWalkable;
        [SerializeField] bool hasDoubleJumped;
        public NetworkVariable<bool> canDoubleJump = new(false);
        public NetworkVariable<bool> canWallRun = new(false);

        [SerializeField] float wallRunSpeed, wallRunMaxCamTilt, wallRunCurrentCamTilt;
        [SerializeField] Collider ownCollider;
        enum WallRunSide
        {
            none = 0,
            left = 1,
            right = 2
        }
        [SerializeField] WallRunSide wallRunSide;
        [SerializeField] AnimationCurve wallRunHeight;
        [SerializeField] Vector3 addedVelocity;
        [SerializeField] Vector3 groundNormal;
        [SerializeField] float downwardVelocity;
        private void Start()
        {
            if (IsOwner)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                pim.QueryControls().WorldInput.Jump.performed += (InputAction.CallbackContext context) => { JumpCheck(); };
                pim.QueryControls().WorldInput.Pause.performed += (InputAction.CallbackContext context) => { TogglePause(); };
            }
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            maxMoveSpeed.OnValueChanged += (float oldValue, float newValue) => { maxMoveSpeed.Value = newValue; };
            moveSpeed.OnValueChanged += (float oldValue, float newValue) => { moveSpeed.Value = newValue; };
            canWallRun.OnValueChanged += (bool oldValue, bool newValue) => { canWallRun.Value = newValue; };
            canDoubleJump.OnValueChanged += (bool oldValue, bool newValue) => { canDoubleJump.Value = newValue; };
            airAcceleration.OnValueChanged += (float oldValue, float newValue) => { airAcceleration.Value = newValue; };

        }
        void TogglePause()
        {
            PauseMenu.instance.TogglePauseMenu(!PauseMenu.instance.paused);
        }
        private void FixedUpdate()
        {

                grounded = CheckGround();
                if (grounded)
                {
                    hasDoubleJumped = false;
                }
                else
                {

                }
            if (IsOwner && !PauseMenu.instance.paused)
            {
                CameraRotation();
                Movement();
            }
            

        }
        void Movement()
        {
            Vector3 lateralVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            Vector3 moveVector = Time.fixedDeltaTime * (transform.rotation * new Vector3(pim.moveInput.x, 0, pim.moveInput.y));

            if((grounded && lateralVelocity.magnitude < maxMoveSpeed.Value))
            {
                rb.MovePosition(transform.position + moveSpeed.Value * rb.mass * moveVector);
            }
            else
            {
                rb.AddForce(moveVector * airAcceleration.Value);
            }
        }
        void CameraRotation()
        {
            if (head)
            {
                headAngle += -pim.lookInput.y * lookSpeed * (invertHeadRotation ? 1 : -1) * Time.fixedDeltaTime;
                headAngle = Mathf.Clamp(headAngle, -89.5f, 89.5f);
                head.transform.localEulerAngles = Vector3.right * headAngle;
            }
            transform.Rotate(transform.up * lookSpeed * pim.lookInput.x * Time.fixedDeltaTime);
        }
        void JumpCheck()
        {
            if (grounded)
            {
                Jump();
            }
            else
            {
                if(canDoubleJump.Value && !hasDoubleJumped)
                {
                    Jump();
                    hasDoubleJumped = true;
                }
            }
        }
        void Jump()
        {
            if (!PauseMenu.instance.paused && QuantumConsole.Instance.GetComponentInChildren<Canvas>().isActiveAndEnabled)
            {
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                rb.AddForce(transform.up * jumpForce, ForceMode.VelocityChange);
            }
        }
        /// <summary>
        /// Check if the player is grounded, performs an overlap check just below the feet of the player, smaller than the radius of the player.
        /// </summary>
        bool CheckGround()
        {
            bool grounded = Physics.CheckSphere(transform.position + ((groundCheckRadius * 0.5f) * transform.up), groundCheckRadius, terrainMask);
            groundWalkable = GetGroundNormal();
            
            return grounded;

        }
        WallRunSide CheckWalls()
        {
            WallRunSide currentSide = 0;
            if (!canWallRun.Value)
                return 0;


            return currentSide;
        }
        /// <summary>
        /// Returns true if the ground is walkable.
        /// </summary>
        /// <returns></returns>
        bool GetGroundNormal()
        {
            groundNormal = Vector3.up;
            Vector3 averageGroundNormal = Vector3.up;
            Vector3 raycastOrigin = Vector3.right * groundCheckRadius;
            Physics.Raycast(groundCheckOrigin.position, -transform.up, out RaycastHit hit, groundCheckRadius * 2);
            if (hit.collider)
                averageGroundNormal = hit.normal;
            Physics.Raycast(groundCheckOrigin.position + (Vector3.right * groundCheckRadius), -transform.up, out hit, groundCheckRadius * 2);
            if (hit.collider)
                averageGroundNormal += hit.normal;
            Physics.Raycast(groundCheckOrigin.position + (Vector3.forward * groundCheckRadius), -transform.up, out hit, groundCheckRadius * 2);
            if (hit.collider)
                averageGroundNormal += hit.normal;
            Physics.Raycast(groundCheckOrigin.position + (Vector3.left * groundCheckRadius), -transform.up, out hit, groundCheckRadius * 2);
            if (hit.collider)
                averageGroundNormal += hit.normal;
            Physics.Raycast(groundCheckOrigin.position + (Vector3.back * groundCheckRadius), -transform.up, out hit, groundCheckRadius * 2);
            if (hit.collider)
                averageGroundNormal += hit.normal;

            averageGroundNormal.Normalize();
            groundNormal = averageGroundNormal;
            float gnDot = Vector3.Dot(groundNormal, Vector3.up);
            return gnDot >= 0.5f;
        }
        private void OnDrawGizmos()
        {
            Gizmos.DrawWireSphere(groundCheckOrigin.position, groundCheckRadius);
        }
    }
}
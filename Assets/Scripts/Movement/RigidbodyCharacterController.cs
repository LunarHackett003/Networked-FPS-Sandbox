using QFSW.QC;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http.Headers;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Eclipse.Gameplay
{
    public class RigidbodyCharacterController : NetworkBehaviour
    {
        [SerializeField] Input.PlayerInputManager pim;
        [SerializeField] Rigidbody rb;
        enum MoveState
        {
            Walk = 0,
            Slide = 1,
            WallRun = 2
        }

        [Space]
        [SerializeField, Header("Physics")] PhysicMaterial defaultPhysicMaterial;
        [SerializeField] PhysicMaterial specialMovePhysicMaterial;
        [SerializeField] bool sprinting;
        [SerializeField] MoveState moveState;
        [SerializeField] Collider playerCollider;
        [SerializeField] bool grounded;
        [SerializeField] bool sliding;
        [SerializeField] Transform groundCheckOrigin;
        [SerializeField] Transform slopeCheckOrigin;
        [SerializeField] LayerMask terrainMask;
        [SerializeField] float groundCheckRadius,groundCheckDistance;
        [SerializeField] Transform head;
        [SerializeField] protected float headAngle;
        [Command]
        public float lookSpeed = 15f;
        [Command]
        public bool invertHeadRotation;
        public NetworkVariable<float> walkForce = new(50);
        public NetworkVariable<float> maxWalkSpeed = new(8);
        public NetworkVariable<float> sprintForce = new(75);
        public NetworkVariable<float> maxSprintSpeed = new(12);
        public NetworkVariable<float> specialMoveDrag = new(0.1f);
        public NetworkVariable<float> jumpForce = new(5f);
        public NetworkVariable<float> regularMoveDrag = new(5f);
        public NetworkVariable<float> airAcceleration = new(10f);
        public NetworkVariable<float> wallRunGravity = new(-0.9f), wallRunGravityCounterTime = new(4f);
        [SerializeField] bool hasDoubleJumped;
        public NetworkVariable<bool> canDoubleJump = new(false);
        public NetworkVariable<bool> canWallRun = new(false);
        [SerializeField] float wallCheckDistance;
        [SerializeField] float wallStickForce;
        RaycastHit slopeHit = new();
        RaycastHit wallhit = new();
        Vector3 slopeMoveDirection;
        Vector3 moveDirection;
        [SerializeField] bool wallRunning;
        float wallrunCooldown = 0.1f;
        [SerializeField] float wallRunTiltSpeed, wallRunTiltAngle, wallRunTargetTilt;
        float headtilt;
        [SerializeField] Transform crouchMoveTransform;
        [SerializeField] Vector3 crouchTransformInitialPosition, crouchTransformFinalPosition;
        [SerializeField] bool crouched;
        [SerializeField] float crouchSpeed = 5, colliderCrouchHeight = 1.4f, crouchColliderCentreHeight = 0.7f, baseColliderCentreHeight = 1;
        [SerializeField] float slideInitalForce;
        [SerializeField] bool onSlope;
        enum WallDirection
        {
            none = 0,
            front = 1,
            left = 2,
            right = 4
        }
        [SerializeField] WallDirection wallDirection = WallDirection.none;
        [SerializeField] WallDirection coolingWallDirection = WallDirection.none;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            crouchTransformInitialPosition = crouchMoveTransform.localPosition;
            Input.Controls controls = pim.QueryControls();
            controls.WorldInput.Jump.performed += (InputAction.CallbackContext context) => { if (!PauseMenu.instance.paused) JumpCheck(); };
            controls.WorldInput.Sprint.performed += (InputAction.CallbackContext context) => { if (!PauseMenu.instance.paused) sprinting = true; crouched = false; };
            controls.WorldInput.Crouch.performed += (InputAction.CallbackContext context) => { if (!PauseMenu.instance.paused) ToggleCrouch(); };
            controls.WorldInput.Pause.performed += (InputAction.CallbackContext context) => { PauseMenu.instance.TogglePauseMenu(!PauseMenu.instance.paused); };

            PauseMenu.instance.TogglePauseMenu(false);
        }
        public override void OnNetworkDespawn()
        {
        }
        private void FixedUpdate()
        {
            
            bool regularMovement = moveState == MoveState.Walk;
            playerCollider.material = (!regularMovement || !grounded) ? specialMovePhysicMaterial : defaultPhysicMaterial;
            rb.drag = ((regularMovement && grounded) || wallRunning) ? regularMoveDrag.Value : specialMoveDrag.Value;
            grounded = CheckGround();
            switch (wallDirection)
            {
                case WallDirection.left:
                    wallRunTargetTilt = wallRunTiltAngle;
                    break;
                case WallDirection.right:
                    wallRunTargetTilt = -wallRunTiltAngle;
                    break;
                default:
                    wallRunTargetTilt = 0;
                    break;
            }
            if(pim.moveInput.y < 0.5f)
            {
                sprinting = false;
            }
            CameraRotation();
            if (grounded || wallDirection != WallDirection.none)
                hasDoubleJumped = false;            
            CheckWall();
            crouchMoveTransform.localPosition = Vector3.Lerp(crouchMoveTransform.localPosition, crouched ? crouchTransformFinalPosition : crouchTransformInitialPosition, crouchSpeed * Time.fixedDeltaTime);

            if (sliding)
            {
                if (rb.LateralVelocity().magnitude > 1f)
                {
                    moveState = MoveState.Slide;
                }
                else
                {
                    moveState = MoveState.Walk;
                    sliding = false;
                }
            }
            if (!wallRunning && !sliding)
                moveState = MoveState.Walk;
            SlopeCheck();
            slopeMoveDirection = Vector3.ProjectOnPlane(moveDirection, slopeHit.normal);
            if(regularMovement && grounded)
            {
                GroundMovement();
            }
            else
            {
                AirMovement();
            }
            if (wallRunning)
            {
                moveState = MoveState.WallRun;
                WallRunProcess();
                sliding = false;
                sprinting = false;
            }
            else
            {
                if (coolingWallDirection != WallDirection.none)
                {
                    wallrunCooldown -= Time.fixedDeltaTime;
                    wallrunCooldown = Mathf.Clamp(wallrunCooldown, 0, .1f);
                    if (wallrunCooldown == 0)
                    {
                        coolingWallDirection = WallDirection.none;
                        wallrunCooldown = .1f;
                    }
                }
            }

        }
        bool CheckGround()
        {
            bool grounded = Physics.CheckSphere(groundCheckOrigin.position, groundCheckRadius, terrainMask);
            if (grounded)
            {
                wallDirection = WallDirection.none;
                coolingWallDirection = WallDirection.none;
            }
            return grounded;
        }
        void GroundMovement()
        {
            if (PauseMenu.instance.paused)
                return;
            moveDirection = (transform.rotation) * new Vector3(pim.moveInput.x, 0, pim.moveInput.y);
            Vector3 moveVector = (sprinting ? sprintForce.Value : walkForce.Value) * slopeMoveDirection;
            rb.AddForce(moveVector, ForceMode.Acceleration);
        }
        void AirMovement()
        {
            if (PauseMenu.instance.paused)
                return;
            moveDirection = (transform.rotation) * new Vector3(pim.moveInput.x, 0, pim.moveInput.y);
            Vector3 moveVector = moveDirection * airAcceleration.Value;
            rb.AddForce(moveVector);
        }
        void CameraRotation()
        {
            if (PauseMenu.instance.paused)
                return;
            headtilt = Mathf.Lerp(headtilt, wallRunTargetTilt, wallRunTiltSpeed * Time.fixedDeltaTime);
            if (head)
            {
                headAngle += -pim.lookInput.y * lookSpeed * (invertHeadRotation ? 1 : -1) * Time.fixedDeltaTime;
                headAngle = Mathf.Clamp(headAngle, -89.5f, 89.5f);
                head.transform.localRotation = Quaternion.Euler(headAngle, 0, headtilt);
            }
            transform.Rotate(transform.up * lookSpeed * pim.lookInput.x * Time.fixedDeltaTime);
        }
        void JumpCheck()
        {
            if (PauseMenu.instance.paused)
                return;
            if (grounded)
            {
                Jump();
            }
            else
            {
                if (canDoubleJump.Value && !hasDoubleJumped)
                {
                    Jump();
                    hasDoubleJumped = true;
                }
            }
        }
        void Jump()
        {

            rb.velocity = rb.LateralVelocity();
            if (!wallRunning)
            {
                rb.AddForce(Vector3.up * jumpForce.Value, ForceMode.Acceleration);
            }
            else
            {
                Vector3 wallJumpDir = wallhit.normal + transform.up;
                rb.AddForce(wallJumpDir.normalized * jumpForce.Value, ForceMode.Acceleration);
                StopWallRun();
            }
            rb.drag = 0;
            playerCollider.material = specialMovePhysicMaterial;
            crouched = false;
        }
        bool SlopeCheck()
        {
            if(Physics.SphereCast(slopeCheckOrigin.position, groundCheckRadius, Vector3.down, out slopeHit, groundCheckDistance, terrainMask))
            {
                onSlope = slopeHit.normal != Vector3.up;
                return onSlope;
            }
            else { return false; }
        }
        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheckOrigin.position, groundCheckRadius);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(slopeCheckOrigin.position, groundCheckRadius);
            Gizmos.DrawWireSphere(slopeCheckOrigin.position + (Vector3.down * groundCheckDistance), groundCheckRadius * 0.9f);
            Gizmos.color = Color.red;
            Gizmos.DrawRay(rb.worldCenterOfMass, transform.right * wallCheckDistance);
            Gizmos.DrawRay(rb.worldCenterOfMass, transform.forward * wallCheckDistance);
            Gizmos.DrawRay(rb.worldCenterOfMass, -transform.right * wallCheckDistance);
        }
        public void CheckWall()
        {
            if (!grounded)
            {
                if (Physics.Raycast(rb.worldCenterOfMass, transform.right, out wallhit, wallCheckDistance, terrainMask))
                    wallDirection = WallDirection.right;
                else if (Physics.Raycast(rb.worldCenterOfMass, -transform.right, out wallhit, wallCheckDistance, terrainMask))
                    wallDirection = WallDirection.left;
                else if (Physics.Raycast(rb.worldCenterOfMass, transform.forward, out wallhit, wallCheckDistance, terrainMask))
                    wallDirection = WallDirection.front;
                else
                    wallDirection = WallDirection.none;

                if (wallDirection != WallDirection.none && wallDirection != coolingWallDirection)
                    StartWallRun();
                else
                {
                    if (wallRunning)
                        StopWallRun();
                }       
            }
            else
            {
                if (wallRunning)
                    StopWallRun();
            }
        }
        void StartWallRun()
        {
            coolingWallDirection = WallDirection.none;
            wallRunning = true;
            rb.useGravity = false;
            rb.velocity = rb.LateralVelocity();
        }
        void StopWallRun()
        {
            coolingWallDirection = wallDirection;
            wallDirection = WallDirection.none;
            wallRunning = false;
            rb.useGravity = true;
        }
        void WallRunProcess()
        {
            if (PauseMenu.instance.paused)
                return;
            rb.AddForce(Vector3.down * wallRunGravity.Value);
            Vector3 wallStickDirection = -wallhit.normal;
            rb.AddForce(wallStickDirection * wallStickForce);
            Vector3 moveVector = Vector3.Cross(wallhit.normal, transform.up);
            //rb.AddForce(pim.moveInput * sprintForce.Value * moveVector);
            rb.AddForce(pim.moveInput.y * sprintForce.Value * transform.forward, ForceMode.Acceleration);
            if (WallEjectCheck())
            {
                StopWallRun();
                rb.AddForce(wallhit.normal * jumpForce.Value);
            }
        }
        bool WallEjectCheck()
        {
            switch (wallDirection)
            {
                case WallDirection.front:
                    return pim.moveInput.y < -0.3f;
                case WallDirection.left:
                    return pim.moveInput.x > 0.3f;
                case WallDirection.right:
                    return pim.moveInput.x < -0.3f;
                default:
                    return true;
            }
        }
        void ToggleCrouch()
        {
            if (!wallRunning) 
            { 
                crouched = !crouched;
                if ((rb.LateralVelocity().magnitude > maxWalkSpeed.Value * 0.9f || onSlope) && crouched)
                {
                    sliding = true;
                    if (grounded && !onSlope)
                    {
                        InitiateSlide();
                    }
                }
                else
                {
                    sliding = false;
                }
            }
            else
            {
                crouched = false;
            }
        }
        void InitiateSlide()
        {
            rb.AddForce(transform.forward * slideInitalForce, ForceMode.VelocityChange);
            rb.drag = specialMoveDrag.Value;
            playerCollider.material = specialMovePhysicMaterial;
        }
    }
}
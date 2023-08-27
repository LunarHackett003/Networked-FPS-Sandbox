using QFSW.QC;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Eclipse.Input
{
    public class PlayerInputManager : NetworkBehaviour
    {
        private QuantumConsole qc;

        public bool inMenu;
        public Vector2 moveInput;
        public bool jumping;
        public bool fireInput;
        public Vector2 lookInput;
        [SerializeField] protected float headAngle;
        [Command]
        public float moveSpeed = 2f;
        [Command]
        public float lookSpeed = 15f;
        public (Quaternion bodyRot, Quaternion localHead) CharacterRotation()
        {
            return (transform.rotation, head.localRotation);
        }
        [SerializeField] protected Transform head;
        protected Controls controls;
        private void OnEnable()
        {
            if (!IsOwner)
                return;
            controls = new();
            controls.Enable();
            GetComponentInChildren<Camera>().gameObject.SetActive(IsOwner);
            
        }
        private void OnDisable()
        {
            if (!IsOwner)
                return;
            controls.Disable();
            controls.Dispose();
        }

        private void Update()
        {
            if (!IsOwner)
                return;
            inMenu = QuantumConsole.Instance.transform.GetChild(0).gameObject.activeInHierarchy;
            if (!inMenu)
            {
                moveInput = controls.WorldInput.Move.ReadValue<Vector2>();
                lookInput = controls.WorldInput.Aim.ReadValue<Vector2>();
            }
            else
            {
                moveInput = Vector2.zero;
                lookInput = Vector2.zero;
            }
        }
        private void FixedUpdate()
        {
            if (!IsOwner)
                return;

             if (head)
             {
                 headAngle += -lookInput.y * lookSpeed * Time.fixedDeltaTime;
                 headAngle = Mathf.Clamp(headAngle, -89.5f, 89.5f);
                 head.transform.localEulerAngles = Vector3.right * headAngle;
             }
             else
             {
                 head.transform.localEulerAngles = Vector3.zero;
             }
             transform.Rotate(transform.up * lookSpeed * lookInput.x * Time.fixedDeltaTime);
             transform.Translate(new Vector3(moveInput.x, 0, moveInput.y) * Time.fixedDeltaTime * moveSpeed, Space.Self);
            
        }
    }
}
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

        [SerializeField] protected Transform head;
        protected Controls controls;
        public Controls QueryControls()
        {
            return controls;
        }
        [Command]
        public void QueryManagerState()
        {
            print($" owned by this player: {IsOwner}, current enabled state: {enabled}");
        }
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (!IsOwner)
            {
                enabled = false;
                GetComponentInChildren<Camera>().gameObject.SetActive(false);
            }
        }
        private void OnEnable()
        {
                controls = new();
                controls.Enable();
            
        }
        private void OnDisable()
        {
            if (IsOwner)
            {
                controls.Disable();
                controls.Dispose();
            }
        }

        private void Update()
        {

            inMenu = QuantumConsole.Instance.transform.GetChild(0).gameObject.activeInHierarchy;
            if (IsOwner && !inMenu)
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

        }

        [ServerRpc]
        private void ExampleServerRpc()
        {

        }
    }
}
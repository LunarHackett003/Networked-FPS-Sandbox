using Eclipse.Input;
using QFSW.QC;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Eclipse.Weapons.Firearms.Dev
{
    public class NetTestFirearm : NetworkBehaviour
    {
        [SerializeField] protected Rigidbody bullet;
        [SerializeField] protected Transform fireOrigin;
        [SerializeField] protected float bulletVelocity;
        private void Start()
        {
            if(IsOwner)
                GetComponentInParent<PlayerInputManager>().QueryControls().WorldInput.Fire.performed += x => { FireServerRpc(); };
        }

        [ServerRpc]
        public void FireServerRpc()
        {
            Rigidbody blt = Instantiate(bullet, fireOrigin.position, Quaternion.identity);
            blt.GetComponent<NetworkObject>().Spawn(true);
            blt.velocity = fireOrigin.forward * bulletVelocity;
            blt.transform.forward = blt.velocity;
        }
    }
}
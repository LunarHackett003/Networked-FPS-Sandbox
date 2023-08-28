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
        [SerializeField] protected bool playerGun;
        [SerializeField] protected Rigidbody bullet;
        [SerializeField] protected Transform fireOrigin;
        [SerializeField] protected float bulletVelocity;

        [SerializeField] protected bool spawnWithOwnership;

        public override void OnNetworkSpawn()
        {
            if (GetComponentInParent<PlayerInputManager>())
            {
                playerGun = true;
                fireOrigin = GetComponentInParent<FirearmManager>().GetFireOrigin();
            }
        }
        private void Start()
        {
            if (IsOwner && playerGun)
            {
                GetComponentInParent<PlayerInputManager>().QueryControls().WorldInput.Fire.performed += x => { if (spawnWithOwnership) FireSelfOwner(); else FireServerRpc(); };    
            }
        }

        [ServerRpc]
        public void FireServerRpc()
        {
            Rigidbody blt = Instantiate(bullet, fireOrigin.position, Quaternion.identity);
            blt.GetComponent<NetworkObject>().Spawn(true);
            blt.velocity = fireOrigin.forward * bulletVelocity;
            blt.transform.forward = blt.velocity;
        }
        public void FireSelfOwner()
        {
            Rigidbody blt = Instantiate(bullet, fireOrigin.position, Quaternion.identity);
            blt.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId, true);
            blt.velocity = fireOrigin.forward * bulletVelocity;
            blt.transform.forward = blt.velocity;
        }
    }
}
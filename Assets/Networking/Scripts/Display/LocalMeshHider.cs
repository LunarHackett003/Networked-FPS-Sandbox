using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Eclipse.Networking
{
    public class LocalMeshHider : NetworkBehaviour
    {
        [SerializeField] Transform[] transformsToHideIfLocal;
        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if(IsOwner)
                foreach (Transform t in transformsToHideIfLocal)
                {
                    t.gameObject.SetActive(false);
                }
        }
    }
}
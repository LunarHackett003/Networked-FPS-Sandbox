using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class NetworkTimedDestroy : NetworkBehaviour
{
    public float destroyTime;
    // Start is called before the first frame update
    void Start()
    {
        StartCoroutine(KillCR());
    }
    [ServerRpc]
    void KillThisObjectServerRpc()
    {
        GetComponent<NetworkObject>().Despawn(true);
    }
    IEnumerator KillCR()
    {
        yield return new WaitForSeconds(destroyTime);
        KillThisObjectServerRpc();
    }
}

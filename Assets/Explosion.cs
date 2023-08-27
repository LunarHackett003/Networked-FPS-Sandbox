using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{

    public float explosionRange;
    public float explosionForce;
    private void OnGUI()
    {
        if(GUI.Button(new Rect(100, 100, 200, 200), new GUIContent("Kaaaaabooooom!")))
        {
            Explode();
        }
    }

    public void Explode()
    {
        foreach (var item in Physics.OverlapSphere(transform.position, explosionRange))
        {
            if (item.attachedRigidbody)
            {
                item.attachedRigidbody.AddExplosionForce(explosionForce, transform.position, explosionRange, 0.1f, ForceMode.Impulse);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRange);
        if (explosionForce > 0)
        {
            Gizmos.color = Color.green;
        }
        else
        {
            Gizmos.color= Color.red;
        }
        Gizmos.DrawRay(transform.position, explosionForce * Vector3.right);
        Gizmos.DrawRay(transform.position, explosionForce * Vector3.left);
        Gizmos.DrawRay(transform.position, explosionForce * Vector3.up);
        Gizmos.DrawRay(transform.position, explosionForce * Vector3.down);
        Gizmos.DrawRay(transform.position, explosionForce * Vector3.back);
        Gizmos.DrawRay(transform.position, explosionForce * Vector3.forward);
    }
}

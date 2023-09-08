using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OutOfBoundsResetter : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.attachedRigidbody)
        {
            transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
        }
    }
}

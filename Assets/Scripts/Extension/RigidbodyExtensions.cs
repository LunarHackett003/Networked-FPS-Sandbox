using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class RigidbodyExtensions
{
    public static Vector3 LateralVelocity(this Rigidbody body)
    {
        return new Vector3(body.velocity.x, 0, body.velocity.z);
    }
}

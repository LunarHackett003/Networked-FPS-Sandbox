using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace Eclipse.Weapons.Firearms
{
    public class FirearmManager : NetworkBehaviour
    {
        [Tooltip("The camera used by the player as the fire origin; usually the player's head.")]
        [SerializeField] protected Transform cam;
        public Transform GetFireOrigin()
        {
            return cam;
        }
    }
}
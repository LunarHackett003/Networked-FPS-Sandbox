using Eclipse.Character;
using Eclipse.Gameplay;
using QFSW.QC;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

namespace Eclipse.Networking {
    public class NetworkGlobalVariableSetter : NetworkBehaviour
    {
        [Command]
        public float sv_walkForce;
        [Command]
        public float sv_sprintForce;
        [Command] public float sv_jumpForce;
        [Command] public float sv_airAccel;
        [Command] public bool sv_canWallRun;
        [Command] public bool sv_canDoubleJump;
        RigidbodyCharacterController fcc;
        private void FixedUpdate()
        {
            //No network manager or local player object is present, and therefore we cannot execute this code.
            if (!NetworkManager)
                return;
            if ((!IsClient || !IsHost) )
                return;
            if(!NetworkManager.LocalClient.PlayerObject) return;

            if (!fcc)
            {
                fcc = NetworkManager.LocalClient.PlayerObject.GetComponent<RigidbodyCharacterController>();
                return;
            }

            if (IsServer || IsHost)
            {
                fcc.walkForce.Value = sv_walkForce;
                fcc.jumpForce.Value = sv_jumpForce;
                fcc.airAcceleration.Value = sv_airAccel;
                fcc.sprintForce.Value = sv_sprintForce;
                fcc.canWallRun.Value = sv_canWallRun;
                fcc.canDoubleJump.Value = sv_canDoubleJump;
            }
            else
            {
                sv_walkForce = fcc.walkForce.Value;
                sv_airAccel = fcc.airAcceleration.Value;
                sv_sprintForce = fcc.sprintForce.Value;
                sv_canWallRun = fcc.canWallRun.Value;
                sv_jumpForce = fcc.jumpForce.Value;
                sv_canDoubleJump = fcc.canDoubleJump.Value;
            }
        }
        [Command]
        public void QuerySVars()
        {
            Debug.Log($"sv_moveForce:{sv_walkForce}" +
                $"clientMoveForce:{fcc.walkForce.Value}" +
                $"sv_airaccel:{sv_airAccel}" +
                $"clientAirAccel{fcc.airAcceleration.Value}" +
                $"sv_maxMoveSpeed:{sv_sprintForce}" +
                $"clientMaxMoveSpeed:{fcc.sprintForce.Value}" +
                $"sv_canWallRun:{sv_canWallRun}" +
                $"clientCanWallRun:{fcc.canWallRun.Value}" +
                $"sv_canDoubleJump:{sv_canDoubleJump}" +
                $"clientCanDoubleJump:{fcc.canDoubleJump.Value}");
        }
    }
}
using Eclipse.Gameplay;
using Mono.CSharp;
using QFSW.QC;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace Eclipse.Networking.RelayNetworking
{
    public class RelayManager : MonoBehaviour
    {
        [SerializeField] int maxRelayConnections;
        
        public static RelayManager instance;
        private void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
                return;
            }
            instance = this;
        }
        async void Start()
        {
            await UnityServices.InitializeAsync();

            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log($"Signed in with {AuthenticationService.Instance.PlayerId}");
            };
            
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }
        [Command]
        public async void CreateRelay()
        {
            try
            {
                Allocation alloc = await RelayService.Instance.CreateAllocationAsync(maxRelayConnections);

                string joinCode = await RelayService.Instance.GetJoinCodeAsync(alloc.AllocationId);
                Debug.Log($"Join Code: {joinCode}");

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetHostRelayData(
                    alloc.RelayServer.IpV4,
                    (ushort)alloc.RelayServer.Port,
                    alloc.AllocationIdBytes,
                    alloc.Key,
                    alloc.ConnectionData
                    );
                NetworkManager.Singleton.OnClientStarted += () => { ClientStart(); };
                NetworkManager.Singleton.StartHost();
                PauseMenu.instance.joinCodeDisplay.text = joinCode;
            }
            catch (RelayServiceException e)
            {
                Debug.LogException(e);
            }
        }
        /// <summary>
        /// Joins a relay using the join code you were given.
        /// </summary>
        /// <param name="joinCode"></param>
        [Command]
        public async void JoinRelayWithCode(string joinCode)
        {
            try {
                JoinAllocation allocJoined = await RelayService.Instance.JoinAllocationAsync(joinCode);

                NetworkManager.Singleton.GetComponent<UnityTransport>().SetClientRelayData(
                    allocJoined.RelayServer.IpV4,
                    (ushort)allocJoined.RelayServer.Port,
                    allocJoined.AllocationIdBytes,
                    allocJoined.Key,
                    allocJoined.ConnectionData,
                    allocJoined.HostConnectionData);
                NetworkManager.Singleton.StartClient();
                PauseMenu.instance.joinCodeDisplay.text = joinCode;

            }
            catch (RelayServiceException e)
            {
                Debug.LogException(e);
            }
        }
        [Command]
        public void LeaveRelay(ulong clientID)
        {
            ClientDisconnectRPC(clientID);
        }

        public void ClientStart()
        {

        }
        [ServerRpc]
        public void ClientDisconnectRPC(ulong clientID)
        {
            NetworkManager.Singleton.DisconnectClient(clientID, "Requested Disconnect.");
        }
    }
}
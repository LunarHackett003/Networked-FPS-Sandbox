using Mono.CSharp;
using QFSW.QC;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Eclipse.Networking.LobbyNetworking
{
    public class LobbyManager : MonoBehaviour
    {
        public static LobbyManager Instance;

        private int hostMaxPlayerCount = 4;
        private bool privateLobby = false;
        private string hostLobbyName = "Default Lobby Name";
        public void SetMaxPlayerCount(string count)
        {
            int.TryParse(count, out hostMaxPlayerCount);
        }
        public void SetPrivateLobby(bool isPrivate)
        {
            privateLobby = isPrivate;
        }
        public void SetLobbyName(string name)
        {
            hostLobbyName = name;
        }

        public void SetPlayerNameInLobby(Player player)
        {
            if (!player.Data.TryGetValue("playerName", out PlayerDataObject pdo))
            {
                player.Data.TryAdd("playerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Member) { Value = playerName });
            }
        }
        public void SetPlayerName(string name)
        {
            playerName = name;
        }
        string playerName;

        public string clientJoinLobbyCode;

        private Lobby hostLobby;
        bool hostingLobby = false;
        string hostedLobbyID = null;

        private Lobby clientLobby;
        bool clientInLobby = false;
        string clientLobbyID = null;
        private async void Start()
        {
            if (Instance)
            {
                Destroy(gameObject); return;
            }
            else
            {
                Instance = this;
            }
            playerName = $"Default Player{Random.Range(0, 9999999)}";
            await UnityServices.InitializeAsync();

            AuthenticationService.Instance.SignedIn += () =>
            {
                print($"Signed in with PlayerID {AuthenticationService.Instance.PlayerId}.");
            };
            //Currently, use anon sign-in.
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

            //Check if player is connected to any lobbies and disconnect. Connections should not persist after game closure.
           // List<string> joinedLobbies = await Lobbies.Instance.GetJoinedLobbiesAsync();
           // foreach (var item in joinedLobbies)
           // {
           //     await Lobbies.Instance.RemovePlayerAsync(item, AuthenticationService.Instance.PlayerId);
           // }
            
        }

        private float heartbeatTimer;
        private float lobbyPollTimer;
        

        private void FixedUpdate()
        {
            if(hostLobby != null)
                LobbyHeartbeat();

            if (clientInLobby)
                ClientLobbyQuery();
            if (hostingLobby)
                HostLobbyQuery();
        }
        private async void LobbyHeartbeat()
        {
            heartbeatTimer -= Time.fixedDeltaTime;
            if(heartbeatTimer <= 0 )
            {
                heartbeatTimer = 10;
                await Lobbies.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
        private async void LobbyPolling()
        {
            if (!hostingLobby || !clientInLobby)
                return;
            lobbyPollTimer -= Time.fixedDeltaTime;
            if(lobbyPollTimer <= 0)
            {
                lobbyPollTimer = 1.1f;
                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(hostingLobby? hostLobby.Id : clientLobby.Id);
                if (hostingLobby)
                    hostLobby = lobby;
                else
                    clientLobby = lobby;

                if (lobby.HostId == AuthenticationService.Instance.PlayerId)
                {
                    //Transfer current player to host
                    clientLobby = null;
                    hostLobby = lobby;

                    clientLobbyID = null;
                    hostedLobbyID = lobby.Id;

                    clientInLobby = false;
                    hostingLobby = true;
                }
            }
        }
        private void HostLobbyQuery()
        {
            LobbyPolling();
        }
        private void ClientLobbyQuery()
        {
            LobbyPolling();
        }


        [Command]
        public async void CreateLobby()
        {
            try
            {
                CreateLobbyOptions clo = new CreateLobbyOptions()
                {
                    IsPrivate = privateLobby,
                    Player = GetPlayer(),
                    Data = new Dictionary<string, DataObject>
                    {
                        {"gamemode", new DataObject(DataObject.VisibilityOptions.Public, "GamemodeTest") }
                    }
                };

                hostLobby = await LobbyService.Instance.CreateLobbyAsync(hostLobbyName, hostMaxPlayerCount, clo);
                Debug.Log($"{hostLobby.Name} has been created with {hostMaxPlayerCount} slots.");
                hostingLobby = true;
                hostedLobbyID = hostLobby.Id;
                Player p = hostLobby.Players.Find(x => x.Id == AuthenticationService.Instance.PlayerId);
                SetPlayerNameInLobby(p);
                HostLobbyQuery();
                LobbyListUI.instance.LobbyListOnJoin();
            }
            catch (LobbyServiceException e)
            {
                Debug.LogException(e);
            }
        }
        [Command]
        public async void LeaveLobby()
        {
            if (hostingLobby)
            {
                CloseLobby();
                return;
            }
            if (!clientInLobby || string.IsNullOrEmpty(clientLobbyID))
                return;

            try {
                await Lobbies.Instance.RemovePlayerAsync(clientLobbyID, AuthenticationService.Instance.PlayerId);
                Debug.Log($"Left lobby with ID");
            }
            catch(LobbyServiceException e) { Debug.LogException(e); }
        }

        [Command]
        private async void CloseLobby()
        {
            if (!hostingLobby || string.IsNullOrEmpty(hostedLobbyID))
                return;
            try {
                await Lobbies.Instance.DeleteLobbyAsync(hostedLobbyID);
                print($"Closed lobby {hostedLobbyID}");
                hostingLobby = false;
                hostedLobbyID = null;
                hostLobby = null;
                LobbyListUI.instance.LobbyListOnDisconnect();
                }
            catch (LobbyServiceException e)
            {
                Debug.LogException(e);
            }
        }
        [Command]
        public async void ListLobbies()
        {
            try
            {
                QueryResponse query = await Lobbies.Instance.QueryLobbiesAsync();

                Debug.Log($"{query.Results.Count} lobbies found.");
                foreach (Lobby lobby in query.Results)
                {
                    Debug.Log($"{lobby.Name}: {lobby.Players.Count}/{lobby.MaxPlayers}");
                }
            }
            catch(LobbyServiceException e)
            {
                Debug.LogException(e);
            }
        }
        [Command]
        public async void QuickJoinLobby()
        {
            JoinLobbyByIdOptions options = new JoinLobbyByIdOptions()
            {
                Player = GetPlayer()

            };

            try
            {
                QueryResponse qr = await Lobbies.Instance.QueryLobbiesAsync();
                Debug.Log("Found lobby. Joining.");
                clientLobby = await Lobbies.Instance.JoinLobbyByIdAsync(qr.Results[0].Id, options);
                clientInLobby = true;
                clientLobbyID = clientLobby.Id;
                
                ClientLobbyQuery();
                LobbyListUI.instance.LobbyListOnJoin();

            }
            catch (LobbyServiceException e)
            {
                Debug.LogException(e);
            }
        }
        [Command]
        public async void JoinLobbyByCode(string joinCode)
        {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions()
            {
                Player = GetPlayer()
            };
            try
            {
                clientLobby = await Lobbies.Instance.JoinLobbyByCodeAsync(joinCode, options);
                Debug.Log("Joined lobby via code!");
                clientInLobby = true;
                clientLobbyID = clientLobby.Id;
                LobbyListUI.instance.LobbyListOnJoin();

            }
            catch (LobbyServiceException e)
            {
                Debug.LogException(e);
            }
        }
        private Player GetPlayer()
        {
            Player player = new Player(AuthenticationService.Instance.PlayerId)
            {
                Data = new Dictionary<string, PlayerDataObject>
                        {
                            {"playerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public) {Value = playerName } }
                        }
            };
            return player;
        }
        private async void UpdateLobbyGameMode(string newGameMode)
        {
            try
            {
                await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions()
                {
                    Data = new Dictionary<string, DataObject>
                    {
                        {"gamemode", new DataObject(DataObject.VisibilityOptions.Public, newGameMode) }
                    }
                });
            }
            catch (LobbyServiceException e)
            {
                Debug.LogException(e);
            }
        }
        public async Task<List<Lobby>> ReturnLobbyList()
        {
            QueryResponse qr = await Lobbies.Instance.QueryLobbiesAsync();
            List<Lobby> lobbiesAvailable = qr.Results;
            return lobbiesAvailable;
        }
    }
}
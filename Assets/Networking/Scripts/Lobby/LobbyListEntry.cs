using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Networking.LobbyNetworking
{
    public class LobbyListEntry : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI lobbyNameField, gamemodeField, playerCountField;
        [SerializeField] Button joinButton;

        [SerializeField] string lobbyJoinCode;
        public void CreateLobbyListEntry(Lobby targetLobby)
        {
            lobbyJoinCode = targetLobby.LobbyCode;
            lobbyNameField.text = $"Lobby Name: {targetLobby.Name}";
            gamemodeField.text = $"Gamemode: {targetLobby.Data["gamemode"].Value}";
            playerCountField.text = $"Players: {targetLobby.Players.Count}/{targetLobby.MaxPlayers}";
            joinButton.onClick.AddListener(JoinTargetedLobby);
        }
        public void JoinTargetedLobby()
        {
            LobbyManager.Instance.JoinLobbyByCode(lobbyJoinCode);
        }

    }
}
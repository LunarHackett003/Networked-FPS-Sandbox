using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

namespace Eclipse.Networking.LobbyNetworking {
    public class PlayerListEntry : MonoBehaviour
    {
        [SerializeField] TextMeshProUGUI playerNameField;
        public void CreatePlayerListing(Player player)
        {
            playerNameField.text = player.Data["playerName"].Value;
        }
    }
}
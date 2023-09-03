using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Networking.LobbyNetworking {
    public class LobbyListUI : MonoBehaviour
    {
        [SerializeField] Button newLobbyButton, refreshButton;
        [SerializeField] GameObject baseLobbyListing;
        [SerializeField] GameObject basePlayerListing;
        List<GameObject> lobbyListings = new List<GameObject>();
        [SerializeField] Transform listingParent;
        [SerializeField] GameObject newLobbyMenu;
        [SerializeField] Button newLobbyCloseButton;
        private void Awake()
        {
            newLobbyButton.onClick.AddListener(() =>
            {
                newLobbyMenu.SetActive(true);
            });
            refreshButton.onClick.AddListener(() => { PopulateLobbyList(); });
            newLobbyCloseButton.onClick.AddListener(() => { newLobbyMenu.SetActive(false); });
        }

        private void ClearLobbyList()
        {
            for (int i = lobbyListings.Count - 1; i < lobbyListings.Count; i--)
            {
                Destroy(lobbyListings[i]);
            }
        }

        private async void PopulateLobbyList()
        {
            //Clears the list of lobbies available
            ClearLobbyList();
            //Creates new list of lobbies
            List<Lobby> lobbyList = await LobbyManager.Instance.ReturnLobbyList();
            foreach (var item in lobbyList)
            {
                GameObject ll = Instantiate(baseLobbyListing, listingParent);
                ll.GetComponent<LobbyListEntry>().CreateLobbyListEntry(item);
            }
        }
        private async void PopulatePlayerList()
        {
            List<string> joinedLobbies = await Lobbies.Instance.GetJoinedLobbiesAsync();
            Lobby joinedLobby = await Lobbies.Instance.GetLobbyAsync(joinedLobbies[0]);
            List<Player> players = joinedLobby.Players;
            foreach (var item in players)
            {
                GameObject pl = Instantiate(basePlayerListing, listingParent);
                pl.GetComponent<PlayerListEntry>().CreatePlayerListing(item);
            }
        }
    }
}
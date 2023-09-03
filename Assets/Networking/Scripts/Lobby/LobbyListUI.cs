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

        private async void PopulateLobbyList()
        {
            //Clears the list of lobbies available
            for (int i = lobbyListings.Count - 1; i < lobbyListings.Count; i--)
            {
                Destroy(lobbyListings[i]);
            }
            //Creates new list of lobbies
            List<Lobby> lobbyList = await LobbyManager.Instance.ReturnLobbyList();
            foreach (var item in lobbyList)
            {
                GameObject ll = Instantiate(baseLobbyListing, listingParent);
                ll.GetComponent<LobbyListEntry>().CreateLobbyListEntry(item);
            }
        }
    }
}
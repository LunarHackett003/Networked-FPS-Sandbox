using Mono.CSharp;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Networking.LobbyNetworking {
    public class LobbyListUI : MonoBehaviour
    {

        public static LobbyListUI instance;

        [SerializeField] Button newLobbyButton, refreshButton;
        [SerializeField] GameObject baseLobbyListing;
        [SerializeField] GameObject basePlayerListing;
        List<GameObject> lobbyListings = new List<GameObject>();
        [SerializeField] Transform listingParent;
        [SerializeField] GameObject newLobbyMenu;
        [SerializeField] Button newLobbyCloseButton;
        [SerializeField] Button gameStartButton;
        private void Awake()
        {
            if (instance)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                instance = this;
            }

            newLobbyButton.onClick.AddListener(() =>
            {
                newLobbyMenu.SetActive(true);
            });
            refreshButton.onClick.AddListener(() => { PopulateLobbyList(); });
            newLobbyCloseButton.onClick.AddListener(() => { newLobbyMenu.SetActive(false); });
        }

        private void ClearLobbyList()
        {
            if (listingParent.childCount > 0)
            {
                for (int i = listingParent.childCount - 1; i < listingParent.childCount; i--)
                {
                    Destroy(listingParent.GetChild(i).gameObject);
                }
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
        public async void PopulatePlayerList()
        {
            ClearLobbyList();

            List<string> joinedLobbies = await Lobbies.Instance.GetJoinedLobbiesAsync();
            Lobby joinedLobby = await Lobbies.Instance.GetLobbyAsync(joinedLobbies[0]);
            List<Player> players = joinedLobby.Players;
            foreach (var item in players)
            {
                GameObject pl = Instantiate(basePlayerListing, listingParent);
                pl.GetComponent<PlayerListEntry>().CreatePlayerListing(item);
            }
        }
        public void LobbyListOnJoin()
        {
            refreshButton.enabled = false;
            newLobbyButton.enabled = false;
            StartCoroutine(DelayListQuery(0));
        }
        public void LobbyListOnDisconnect()
        {
            refreshButton.enabled = true;
            newLobbyButton.enabled = true;
            StartCoroutine(DelayListQuery(1));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"> 0 triggers player list, 1 triggers lobby list</param>
        /// <returns></returns>
        IEnumerator DelayListQuery(int type)
        {
            yield return new WaitForSeconds(5);
            switch (type)
            {
                case 0: PopulatePlayerList(); break;
                case 1: PopulateLobbyList(); break;
                default: break;
            }
        }
    }
}
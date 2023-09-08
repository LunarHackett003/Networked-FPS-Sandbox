using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Eclipse.Gameplay
{
    public class PauseMenu : MonoBehaviour
    {
        public static PauseMenu instance;
        public TextMeshProUGUI joinCodeDisplay;
        public TMP_InputField joinCodeField;
        public Button createGameButton;
        public Button joinGameButton;
        public Button disconnectButton;
        public bool paused;
        private void Start()
        {
            if (instance)
            {
                Destroy(gameObject);
                return;
            }
            else
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }



            createGameButton.onClick.AddListener(() => { Networking.RelayNetworking.RelayManager.instance.CreateRelay(); });
            joinGameButton.onClick.AddListener(() => { Networking.RelayNetworking.RelayManager.instance.JoinRelayWithCode(joinCodeField.text); });
            disconnectButton.onClick.AddListener(() => { Networking.RelayNetworking.RelayManager.instance.LeaveRelay(NetworkManager.Singleton.LocalClientId); });
        }

        public void TogglePauseMenu(bool paused)
        {
            gameObject.SetActive(paused);
            this.paused = paused;

            if (!paused)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
{
    [SerializeField] protected Button clientButton, hostButton, disconnectButton;
    private void Awake()
    {
        if(clientButton)
        clientButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartClient();
            
        });
        if(hostButton)
        hostButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.StartHost();
        });
        if (disconnectButton)
            disconnectButton.onClick.AddListener(() => {
                if (NetworkManager.Singleton.IsClient)
                    NetworkManager.Singleton.DisconnectClient(NetworkManager.Singleton.LocalClientId, "Quit Server.");
                else
                {
                    NetworkManager.Singleton.Shutdown();
                    Debug.Log("Server closed by Host.");
                }
            });
    }
    
}

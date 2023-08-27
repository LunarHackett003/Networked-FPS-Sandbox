using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetworkUI : MonoBehaviour
{
    [SerializeField] protected Button clientButton, hostButton;
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
    }
    
}

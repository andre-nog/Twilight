using UnityEngine;
using Unity.Netcode;

/// <summary>
/// Habilita o AudioListener apenas para o jogador local.
/// Evita conflito de múltiplos listeners em multiplayer.
/// </summary>
[RequireComponent(typeof(AudioListener))]
public class AudioListenerEnabler : MonoBehaviour
{
    void Start()
    {
        // Aguarda conexão válida
        if (!NetworkManager.Singleton || !NetworkManager.Singleton.IsConnectedClient)
            return;

        // Verifica se este objeto pertence ao jogador local
        var netObj = GetComponentInParent<NetworkObject>();
        if (netObj == null) return;

        bool isOwner = netObj.OwnerClientId == NetworkManager.Singleton.LocalClientId;

        // Ativa o AudioListener apenas se for do jogador local
        var listener = GetComponent<AudioListener>();
        if (listener != null)
            listener.enabled = isOwner;
    }
}

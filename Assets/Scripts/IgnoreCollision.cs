using UnityEngine;

public class IgnoreCollision : MonoBehaviour
{
    void Start()
    {
        Debug.Log("[IgnoreCollision] Start() executado.");

        int playerLayer = LayerMask.NameToLayer("Player");
        int healingLayer = LayerMask.NameToLayer("Ignore Collision");

        if (playerLayer == -1)
        {
            Debug.LogWarning("[IgnoreCollision] Layer 'Player' não encontrada.");
        }

        if (healingLayer == -1)
        {
            Debug.LogWarning("[IgnoreCollision] Layer 'Ignore Collision' não encontrada.");
        }

        if (playerLayer != -1 && healingLayer != -1)
        {
            Physics.IgnoreLayerCollision(playerLayer, healingLayer);
            Physics.IgnoreLayerCollision(healingLayer, playerLayer); // redundância segura
            Debug.Log($"[IgnoreCollision] Colisão ignorada entre Player (Layer {playerLayer}) e HealingZone (Layer {healingLayer})");
        }
        else
        {
            Debug.LogError("[IgnoreCollision] Erro ao ignorar colisão: layers inválidas.");
        }
    }
}
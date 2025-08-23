using System;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRegistry : MonoBehaviour
{
    // Lista global de players (lado do servidor mantém isso atualizado)
    public static readonly List<PlayerActor> AllPlayers = new();

    // Eventos para avisar quando entra/sai player (usado pela IA para retarget imediato)
    public static event Action<PlayerActor> OnPlayerRegistered;
    public static event Action<PlayerActor> OnPlayerUnregistered;

    public static void Register(PlayerActor player)
    {
        if (player == null) return;
        if (AllPlayers.Contains(player)) return;

        AllPlayers.Add(player);
        OnPlayerRegistered?.Invoke(player); // ✅ dispara quando um player entra
        // Debug.Log($"[PlayerRegistry] Registered: {player.name} (total={AllPlayers.Count})");
    }

    public static void Unregister(PlayerActor player)
    {
        if (player == null) return;
        if (!AllPlayers.Contains(player)) return;

        AllPlayers.Remove(player);
        OnPlayerUnregistered?.Invoke(player); // ✅ dispara quando um player sai
        // Debug.Log($"[PlayerRegistry] Unregistered: {player.name} (total={AllPlayers.Count})");
    }

    // Helper opcional: pega o player vivo mais próximo de um ponto
    public static PlayerActor GetClosestAliveTo(Vector3 origin)
    {
        PlayerActor best = null;
        float bestDistSq = float.MaxValue;

        foreach (var p in AllPlayers)
        {
            if (p == null || !p.gameObject.activeInHierarchy) continue;
            if (p.CurrentHealth.Value <= 0f) continue;

            float dSq = (p.transform.position - origin).sqrMagnitude;
            if (dSq < bestDistSq) { bestDistSq = dSq; best = p; }
        }
        return best;
    }
}
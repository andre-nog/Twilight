// Assets/Scripts/Enemy/Boss/SpellEventReceiver.cs
using UnityEngine;

/// <summary>
/// Deve ser colocado no GameObject que contém o clip (ex: "Base mesh MonsterMutant7 skin4").
/// Repasse os Animation Events ao BossSpellCast na raiz.
/// </summary>
public class SpellEventReceiver : MonoBehaviour
{
    private BossSpellCast boss;

    void Awake()
    {
        boss = GetComponentInParent<BossSpellCast>();
        if (boss == null)
            Debug.LogError("[SpellEventReceiver] Não encontrou BossSpellCast em nenhum parent!");
    }

    // Estes métodos devem corresponder exatamente ao nome do Animation Event
    public void OnSpellFrame()
    {
        boss?.OnSpellFrame();
    }

    public void OnSpellFrameMeteor()
    {
        boss?.OnSpellFrameMeteor();
    }
}
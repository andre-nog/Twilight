using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CooldownSkillUI : MonoBehaviour
{
    [Header("Refs")]
    public Image cooldownOverlay;
    public TextMeshProUGUI cooldownText;

    [Header("Spell Asset")]
    public SpellBase spellData;            // arraste o .asset da habilidade aqui

    private float cooldownTime;            // valor copiado do spell
    private float cooldownRemaining = 0f;
    private bool  isOnCooldown      = false;

    /* -------------------------------------------------- */
    void Awake()
    {
        cooldownTime = spellData != null ? spellData.Cooldown : 5f;
        cooldownOverlay.fillAmount = 0f;
    }

    void Update()
    {
        if (!isOnCooldown) return;

        cooldownRemaining -= Time.deltaTime;
        cooldownOverlay.fillAmount = cooldownRemaining / cooldownTime;

        if (cooldownText != null)
            cooldownText.text = Mathf.Ceil(cooldownRemaining).ToString();

        if (cooldownRemaining <= 0f)
        {
            isOnCooldown = false;
            cooldownOverlay.fillAmount = 0f;
            if (cooldownText != null) cooldownText.text = "";
        }
    }

    /* -------------------------------------------------- */
    public void TriggerCooldown()
    {
        if (isOnCooldown || spellData == null) return;

        cooldownTime      = spellData.Cooldown;   // garante valor atualizado
        cooldownRemaining = cooldownTime;
        isOnCooldown      = true;

        cooldownOverlay.fillAmount = 1f;
        if (cooldownText != null)
            cooldownText.text = Mathf.Ceil(cooldownTime).ToString();
    }

    public bool IsOnCooldown() => isOnCooldown;
}
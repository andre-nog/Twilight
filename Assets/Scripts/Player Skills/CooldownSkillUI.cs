using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CooldownSkillUI : MonoBehaviour
{
    [Header("Cooldown Settings")]
    public Image cooldownOverlay;
    public TextMeshProUGUI cooldownText;
    public float cooldownTime = 5f;

    private float cooldownRemaining = 0f;
    private bool isOnCooldown = false;

    void Update()
    {
        if (isOnCooldown)
        {
            cooldownRemaining -= Time.deltaTime;
            cooldownOverlay.fillAmount = cooldownRemaining / cooldownTime;

            if (cooldownText != null)
                cooldownText.text = Mathf.Ceil(cooldownRemaining).ToString();

            if (cooldownRemaining <= 0f)
            {
                isOnCooldown = false;
                cooldownOverlay.fillAmount = 0f;
                if (cooldownText != null)
                    cooldownText.text = "";
            }
        }
    }

    public void TriggerCooldown()
    {
        isOnCooldown = true;
        cooldownRemaining = cooldownTime;
        cooldownOverlay.fillAmount = 1f;
        if (cooldownText != null)
            cooldownText.text = Mathf.Ceil(cooldownTime).ToString();
    }

    public bool IsOnCooldown()
    {
        return isOnCooldown;
    }
}
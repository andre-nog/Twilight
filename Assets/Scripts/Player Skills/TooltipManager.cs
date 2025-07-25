using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TooltipManager : MonoBehaviour
{
    /* ---------- Estrutura de cada entrada ---------- */
    [System.Serializable]
    public class SkillTooltip
    {
        public string          label;      // nome só para organização no Inspector
        public RectTransform   icon;       // ícone / botão na UI
        public ScriptableObject spellData; // ProjectileSpell, TeleportSpell, etc.
    }

    /* ---------- Referências ---------- */
    public Canvas            parentCanvas;
    public RectTransform     toolTipTransform;
    public TextMeshProUGUI   tooltipText;
    public PlayerStats       playerStats;
    public List<SkillTooltip> skillTooltips = new();

    private Camera uiCam;

    /* ---------- Lifecycle ---------- */
    void Awake()
    {
        uiCam = Camera.main;
        Hide();
    }

    void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        /* Usa câmera = null se o canvas é Overlay */
        Camera cam = parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                     ? null
                     : parentCanvas.worldCamera;

        /* Move tooltip perto do ponteiro */
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentCanvas.transform as RectTransform,
            mousePos,
            cam,
            out Vector2 localPos);

        toolTipTransform.localPosition = localPos + new Vector2(15f, -15f);

        /* Hover – procura qual ícone está debaixo do mouse */
        foreach (var entry in skillTooltips)
        {
            if (entry.icon == null || entry.spellData == null) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(
                    entry.icon, mousePos, cam))
            {
                Show(entry.spellData);
                return;    // já exibiu
            }
        }

        Hide();            // mouse não está em nenhum ícone
    }

    /* ---------- Mostrar / ocultar ---------- */
    private void Show(ScriptableObject spellData)
    {
        tooltipText.text = BuildTooltip(spellData);
        toolTipTransform.gameObject.SetActive(true);
    }

    private void Hide() => toolTipTransform.gameObject.SetActive(false);

    /* ---------- Construtor de texto ---------- */
    private string BuildTooltip(ScriptableObject spellData)
    {
        if (spellData == null || playerStats == null) return "";

        string description = "";
        float  finalDamage = 0f;
        float  manaCost    = 0f;
        float  range       = 0f;
        string formula     = "";

        /* -------- ProjectileSpell (ex.: Fireball) -------- */
        if (spellData is ProjectileSpell proj)
        {
            float baseDmg   = proj.DamageAmount;
            finalDamage     = DamageCalculator.CalculateFireballDamage(baseDmg, playerStats);

            /* scaling calculado automaticamente */
            float scaling   = playerStats.Intelligence > 0
                              ? (finalDamage - baseDmg) / playerStats.Intelligence
                              : 0f;

            formula         = DamageCalculator.GetFormulaText(baseDmg, scaling, playerStats.Intelligence);

            description = proj.Description;
            manaCost    = proj.ManaCost;
            range       = proj.Range;
        }
        /* -------- TeleportSpell (não causa dano) -------- */
        else if (spellData is TeleportSpell tele)
        {
            description = tele.Description;
            manaCost    = tele.ManaCost;
            range       = tele.Range;
        }

        /* Substitui placeholders */
        return description
            .Replace("{DANO}",   $"<color=#FFCC66>{finalDamage:F0}</color>")
            .Replace("{RANGE}",  $"<color=#99FF99>{range:F1}</color>")
            .Replace("{MANA}",   $"<color=#00BFFF>{manaCost:F0}</color>")
            .Replace("{CALCULO}", formula);
    }
}
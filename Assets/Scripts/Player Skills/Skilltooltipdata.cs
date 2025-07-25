using UnityEngine;

[System.Serializable]
public class SkillTooltipData
{
    public string Label = "Habilidade";     // ← Aparece no Inspector
    public RectTransform icon;
    public SpellBase spellData;
}

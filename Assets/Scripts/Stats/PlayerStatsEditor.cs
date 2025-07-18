#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerStats))]
public class PlayerStatsEditor : Editor
{
public override void OnInspectorGUI()
{
    DrawDefaultInspector();

    var stats = (PlayerStats)target;

    // --- Seção de Mana ---
    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Mana", EditorStyles.boldLabel);
    EditorGUILayout.LabelField(
        "Current / Max Mana",
        $"{stats.CurrentMana:F0} / {stats.MaxMana:F0}"
    );
    EditorGUILayout.LabelField(
        "Mana Regen",
        stats.ManaRechargeRate.ToString("F2")
    );

    // --- Derived Stats ---
    EditorGUILayout.Space();
    EditorGUILayout.LabelField("Derived Stats", EditorStyles.boldLabel);
    EditorGUILayout.LabelField(
        "Final Attack Damage",
        stats.FinalAttackDamage.ToString("F2")
    );
    EditorGUILayout.LabelField(
        "Final Attack Speed",
        stats.FinalAttackSpeed.ToString("F2")
    );
}
}
#endif
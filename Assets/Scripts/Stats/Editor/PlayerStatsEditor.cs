using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PlayerStats))]
public class PlayerStatsEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        PlayerStats stats = (PlayerStats)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("=== Debug ===", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Inteligência atual:", stats.Intelligence.ToString());

        if (stats.fireballData != null)
        {
            float danoFinal = DamageCalculator.CalculateFireballDamage(
                stats.fireballData.DamageAmount, stats);
            EditorGUILayout.LabelField("Dano da Fireball:", danoFinal.ToString("F1"));
        }
        else
        {
            EditorGUILayout.LabelField("ProjectileSpell (fireballData) não atribuído nos Stats.");
        }
    }
}
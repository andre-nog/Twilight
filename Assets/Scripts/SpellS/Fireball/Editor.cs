#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProjectileSpell))]
public class ProjectileSpellEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        ProjectileSpell spell = (ProjectileSpell)target;

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Final Stats (Fireball)", EditorStyles.boldLabel);

        var caster = GameObject.FindWithTag("Player");
        if (caster != null && caster.TryGetComponent<PlayerMagicSystem>(out var magic))
        {
            var stats = magic.GetPlayerStats();
            if (stats != null)
            {
                float final = DamageCalculator.CalculateFireballDamage(spell.DamageAmount, stats);
                EditorGUILayout.LabelField("Final Damage", final.ToString("F2"));
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Player com PlayerMagicSystem não encontrado na cena.", MessageType.Info);
        }
    }
}
#endif
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class FireballRangeAndWidthGizmo : MonoBehaviour
{
    public float range = 8f;
    public float spellRadius = 1f;
    public Color rangeColor = Color.red;
    public Color widthColor = Color.cyan;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Vector3 origin = transform.position;
        Vector3 forward = transform.forward;

        // Círculo de alcance geral
        Handles.color = rangeColor;
        Handles.DrawWireDisc(origin, Vector3.up, range);

        // Círculo no final da seta (impacto)
        Vector3 impactCenter = origin + forward * range;
        Handles.color = widthColor;
        Handles.DrawWireDisc(impactCenter, Vector3.up, spellRadius);
    }
#endif
}
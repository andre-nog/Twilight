using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class RangeCircleGizmo : MonoBehaviour
{
    public float range = 8f;
    public Color color = Color.red;

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Handles.color = color;
        Handles.DrawWireDisc(transform.position, Vector3.up, range);
    }
#endif
}
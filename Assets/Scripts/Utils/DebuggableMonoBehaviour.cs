using UnityEngine;

// Base class to handle debug logging per class
public class DebuggableMonoBehaviour : MonoBehaviour
{
    // Enable or disable logs per instance/class
    [SerializeField]
    public bool enableDebugLogs = false;

    // Central debug log method that respects the flag
    protected void DebugLog(string message)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[{GetType().Name}] {message}");
        }
    }
}

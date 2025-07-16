using UnityEngine;

[AddComponentMenu("Enemy/State Controller")]
public class EnemyState : MonoBehaviour
{
    public bool IsBusy { get; private set; } = false;

    public void SetBusy(bool busy)
    {
        IsBusy = busy;
    }
}
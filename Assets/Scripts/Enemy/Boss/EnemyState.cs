using UnityEngine;

public class EnemyState : MonoBehaviour
{
    public bool IsBusy { get; private set; }

    public void SetBusy(bool busy)
    {
        IsBusy = busy;
    }
}

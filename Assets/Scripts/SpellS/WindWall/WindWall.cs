using UnityEngine;

public class WindWall : MonoBehaviour
{
    [SerializeField] private WindWallSpell windWallData;

    private void Start()
    {
        if (windWallData != null)
            Destroy(gameObject, windWallData.Duration);
        else
        {
            Debug.LogWarning("[WindWall] windWallData está null. Usando fallback de 5 segundos.");
            Destroy(gameObject, 5f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Projectile"))
        {
            Destroy(other.gameObject);
        }
    }
}

using UnityEngine;

/// <summary>
/// Controla a movimentação da câmera seguindo um alvo com suavidade.
/// Deve ser usado com projeção Orthographic ou Perspective.
/// </summary>
public class CameraController : MonoBehaviour
{
    [Header("Alvo a seguir (atribuído em tempo de execução)")]
    public Transform target;

    [Header("Configurações de movimento")]
    public float smoothSpeed = 8f;
    public Vector3 offset = new Vector3(0, 10f, 0);

    void LateUpdate()
    {
        if (target == null)
        {
            // Pode acontecer em frame 0 ou se não foi atribuído ainda
            return;
        }

        Vector3 desiredPosition = target.position + offset;
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
        transform.position = smoothedPosition;
    }

    private void OnDrawGizmosSelected()
    {
        // Gizmo para visualizar offset no editor
        if (target != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(target.position, target.position + offset);
        }
    }
}

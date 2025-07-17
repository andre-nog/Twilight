using UnityEngine;
using UnityEngine.InputSystem; // Novo Input System

public class CursorChanger : MonoBehaviour
{
    [SerializeField] private Vector2 hotspot = new Vector2(16, 16);
    [SerializeField] private float maxRaycastDistance = 100f;

    private Texture2D redCircleCursor;
    private bool isHoveringEnemy = false;

    void Start()
    {
        redCircleCursor = GenerateRedCircleTexture(32, Color.red);
        // Mantém a seta padrão do sistema no início
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    void Update()
    {
        if (Camera.main == null || Mouse.current == null) return;

        // Raycast a partir da posição do mouse
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        Debug.DrawRay(ray.origin, ray.direction * maxRaycastDistance, Color.red);

        // Testa colisão em qualquer layer
        bool hitSomething = Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance);

        // Usa tag para decidir se é um "inimigo"
        bool isNowHovering = hitSomething && hit.collider.CompareTag("Interactable");
        
        if (isNowHovering && !isHoveringEnemy)
        {
            // Entrou no hover de um Interactable
            Cursor.SetCursor(redCircleCursor, hotspot, CursorMode.Auto);
            isHoveringEnemy = true;
        }
        else if (!isNowHovering && isHoveringEnemy)
        {
            // Saiu do hover
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            isHoveringEnemy = false;
        }
    }

    Texture2D GenerateRedCircleTexture(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size);
        tex.filterMode = FilterMode.Point;
        Color transparent = new Color(0, 0, 0, 0);

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                if (Mathf.Abs(dist - radius) <= 1.5f)
                    tex.SetPixel(x, y, color); // borda do círculo
                else
                    tex.SetPixel(x, y, transparent);
            }
        }

        tex.Apply();
        return tex;
    }
}
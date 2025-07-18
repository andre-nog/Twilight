using UnityEngine;
using UnityEngine.InputSystem; // Novo Input System

public class CursorChanger : MonoBehaviour
{
    [SerializeField] private Vector2 hotspot       = new Vector2(16, 16);
    [SerializeField] private float   maxRaycastDistance = 100f;
    [SerializeField] private LayerMask enemyLayer;

    private Texture2D redCircleCursor;
    private bool      isHoveringEnemy = false;

    void Start()
    {
        // Gera o cursor de círculo vermelho
        redCircleCursor = GenerateRedCircleTexture(32, Color.red);

        // Garante que começamos com a seta padrão
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    void Update()
    {
        if (Camera.main == null || Mouse.current == null)
        {
            return;
        }

        // Cria o raio a partir da posição do mouse
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        // Desenha o raio na Scene View
        Debug.DrawRay(ray.origin, ray.direction * maxRaycastDistance, Color.red);

        // 2) Raycast filtrado pela máscara de layer “Enemy”
        bool isNowHovering = Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, enemyLayer);

        // Troca de cursor ao entrar/sair do hover
        if (isNowHovering && !isHoveringEnemy)
        {
            Debug.Log("CursorChanger: Entered enemy hover — setting red circle cursor");
            Cursor.SetCursor(redCircleCursor, hotspot, CursorMode.Auto);
            isHoveringEnemy = true;
        }
        else if (!isNowHovering && isHoveringEnemy)
        {
            Debug.Log("CursorChanger: Exited enemy hover — resetting to default cursor");
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            isHoveringEnemy = false;
        }
    }

Texture2D GenerateRedCircleTexture(int size, Color color)
{
    Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
    tex.filterMode = FilterMode.Point;
    // Não precisamos mais de alphaIsTransparency aqui

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
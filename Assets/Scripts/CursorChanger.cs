using UnityEngine;
using UnityEngine.InputSystem;

public class CursorChanger : MonoBehaviour
{
    [SerializeField] private Vector2 hotspot = new Vector2(16, 16);
    [SerializeField] private float maxRaycastDistance = 100f;
    [SerializeField] private LayerMask enemyLayer;

    private Texture2D redCircleCursor;
    private bool isHoveringEnemy = false;

    void Start()
    {
        redCircleCursor = GenerateRedCircleTexture(32, Color.red);
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
    }

    void Update()
    {
        if (Camera.main == null || Mouse.current == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        Debug.DrawRay(ray.origin, ray.direction * maxRaycastDistance, Color.red);

        bool isNowHovering = Physics.Raycast(ray, out RaycastHit hit, maxRaycastDistance, enemyLayer);

        if (Keyboard.current.aKey.isPressed)
        {
            Cursor.SetCursor(redCircleCursor, hotspot, CursorMode.Auto);
            return;
        }

        if (isNowHovering && !isHoveringEnemy)
        {
            Cursor.SetCursor(redCircleCursor, hotspot, CursorMode.Auto);
            isHoveringEnemy = true;
        }
        else if (!isNowHovering && isHoveringEnemy)
        {
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            isHoveringEnemy = false;
        }
    }

    Texture2D GenerateRedCircleTexture(int size, Color color)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color transparent = new Color(0, 0, 0, 0);
        Vector2 center = new Vector2(size / 2f, size / 2f);
        float radius = size / 2f - 1f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                tex.SetPixel(x, y, Mathf.Abs(dist - radius) <= 1.5f ? color : transparent);
            }
        }

        tex.Apply();
        return tex;
    }
}
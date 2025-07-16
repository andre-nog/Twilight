using UnityEngine;

public class SimpleMissile : MonoBehaviour
{
    public Transform target;        // alvo (Transform do inimigo)
    public float speed = 10f;       // m/s
    public int   damage = 1;

    const float hitRadius = 0.3f;   // distância para considerar impacto
    const float heightOff = 1.0f;   // mira levemente acima do pé

    void Awake()
    {
        // visual laranja simples
        transform.localScale = Vector3.one * 0.25f;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(1f, 0.5f, 0f);      // laranja
        GetComponent<Renderer>().material = mat;
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = target.position + Vector3.up * heightOff;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            speed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPos) < hitRadius)
            HitTarget();
    }

    void HitTarget()
    {
        Actor actor = target.GetComponent<Actor>();
        if (actor != null) actor.TakeDamage(damage);

        Destroy(gameObject);
    }
}
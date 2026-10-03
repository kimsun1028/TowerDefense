using UnityEngine;

public abstract class Projectile : MonoBehaviour
{
    [SerializeField] protected float speed = 10f;
    [SerializeField] protected float arcHeight = 0f;
    [SerializeField] protected bool homing = true;

    protected Enemy target;
    protected int damage;

    private Vector2 startPos;
    private Vector2 endPos;
    private float flightTime;
    private float elapsed;

    public void Init(Enemy target, int damage)
    {
        this.target = target;
        this.damage = damage;
        startPos = transform.position;
        endPos = target.transform.position;
        flightTime = Mathf.Max(0.1f, Vector2.Distance(startPos, endPos) / speed);
    }

    protected virtual void Update()
    {
        // 유도: 살아있는 동안 끝점 계속 갱신, 죽으면 마지막 위치로
        if (homing && target != null) endPos = target.transform.position;

        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / flightTime);

        Vector2 pos = Vector2.Lerp(startPos, endPos, t);
        pos.y += arcHeight * 4f * t * (1f - t);
        transform.position = pos;

        if(t >= 1f)
        {
            if(target != null || !homing) OnHit();
            Destroy(gameObject);
        }
    }

    protected virtual void OnHit()
    {
        // TODO: Enemy 피격 함수 확정 후 교체 (예: target.TakeDamage(damage))
        if (target == null) return;
        Debug.Log($"[Projectile] 명중 -> {target.name}, damage {damage}");
    }
}

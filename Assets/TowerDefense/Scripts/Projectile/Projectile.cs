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
            if(target != null || !homing)
            {
                OnHit();
                PlayHitDetached();
            }
            Destroy(gameObject);
        }
    }

    // 레벨별 외형 교체 (Visual 자식의 Animator)
    public void SetAnimator(RuntimeAnimatorController controller)
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator != null) animator.runtimeAnimatorController = controller;
    }

    // 명중: Visual만 떼어내 Hit 애니메이션을 재생한 뒤 지움
    private void PlayHitDetached()
    {
        Animator animator = GetComponentInChildren<Animator>();
        if (animator == null || animator.runtimeAnimatorController == null) return;

        AnimationClip hitClip = null;
        foreach (AnimationClip clip in animator.runtimeAnimatorController.animationClips)
        {
            if (clip.name.EndsWith("_Hit")) hitClip = clip;
        }
        if (hitClip == null) return;

        Transform visual = animator.transform;
        visual.SetParent(null, true);
        visual.rotation = Quaternion.identity;
        animator.SetTrigger("Hit");
        Destroy(visual.gameObject, hitClip.length);
    }

    protected virtual void OnHit()
    {
        target.TakeDamage(damage);
    }
}

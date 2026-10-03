using UnityEngine;

public class Arrow : Projectile
{
    protected override void Update()
    {
        Vector2 prev = transform.position;
        base.Update();

        Vector2 dir = (Vector2)transform.position - prev;
        if(dir.sqrMagnitude > 0.0001f)
        {
            float angle= Mathf.Atan2(dir.y,dir.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0,0,angle);
        }
    }
}

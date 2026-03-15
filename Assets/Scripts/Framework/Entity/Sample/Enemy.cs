using UnityEngine;
using System.Collections.Generic;

public class Enemy : EntityBase
{
    public float speed = 3;
    public float attackRange = 3f;
    public float attackCooldown = 1.0f;

    GameObject target;
    private float attackTimer;

    bool moving = false;
    bool attacking = false;
    public override void OnStart()
    {
        attackTimer = attackCooldown;
    }
    public override void OnUpdate(float deltaTime)
    {
        if (moving)
            Move(deltaTime);
        if (attacking)
            Attack(deltaTime);
    }
    public override void OnTick(float tickTime)
    {
        Check();
    }
    public void Move(float deltaTime)
    {
        if(target == null) return;
        var targetPos = target.transform.position;
        var direction = (targetPos - transform.position).normalized;
        transform.Translate(direction * speed * deltaTime);
    }
    public void Check()
    {
        target = null;
        var hits = Physics.OverlapSphere(transform.position, 500, LayerMask.GetMask("Player"));
        foreach (var hit in hits)
        {
            // if (hit.gameObject.tag != "Enemy") continue;
            if (hit.gameObject.GetComponent<EntityBase>() != null)
            {
                target = hit.gameObject;
                break;
            }
        }
        if (target == null) return;
        if(Vector3.Distance(transform.position, target.transform.position) < attackRange)
        {
            attacking = true;
            moving = false;
        }
        else
        {
            attacking = false;
            moving = true;
        }
    }
    public void Attack(float deltaTime)
    {
        attackTimer -= deltaTime;
        if (attackTimer > 0) return;

        if (target != null)
        {
            attackTimer = attackCooldown;

            DebugInfo.Log($"攻击{target.name}");
        }
    }
    public void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
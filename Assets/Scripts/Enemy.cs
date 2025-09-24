using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.RuleTile.TilingRuleOutput;

public class Enemy : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private EnemySight sight;
    [SerializeField, InspectorName("Starting Behavior")] private EnemyState behaviorState = EnemyState.Patrol;
    [SerializeField] private float patrolSpeed;
    [SerializeField] private float checkSpeed;
    [SerializeField] private float pursueSpeed;
    [SerializeField] private PatrolPoint[] patrolPoints;
    [SerializeField] private float alertTime;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackCooldown;
    private float pursueTime = 2f;
    private float startPursueTime = 0f;
    private float startAlertTime;
    private Vector2 nextPatrolPoint;
    private Vector2 inspectionTarget;
    private int nextPatrolIndex = 0;
    private bool canMove = true;
    private bool canAttack = true;
    private Direction lookDirection = Direction.Right;
    private GameObject player;

    public enum Direction
    {
        Left,
        Right
    }

    public enum EnemyState
    {
        Patrol,
        Detect,
        Check,
        Search,
        Pursue,
        Attack
    }

    private void Awake()
    {
        nextPatrolPoint = new Vector2(patrolPoints[0].transform.position.x, body.position.y);
        player = GameObject.Find("Player");

        if (behaviorState == EnemyState.Patrol) animator.SetBool("Move", true);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == patrolPoints[nextPatrolIndex].gameObject && behaviorState == EnemyState.Patrol)
        {
            StartCoroutine(ResolvePatrolPoint());
        }
    }

    private void FixedUpdate()
    {
        switch (behaviorState)
        {
            case EnemyState.Patrol:
                Patrol();
                break;
            case EnemyState.Check:
                Check();
                break;
            case EnemyState.Search:
                Search();
                break;
            case EnemyState.Pursue:
                Pursue();
                break;
            case EnemyState.Attack:
                Attack();
                break;
        }
    }

    private void Patrol()
    {
        if (canMove)
            body.velocity = new Vector2(Mathf.Sign(nextPatrolPoint.x - body.position.x) * patrolSpeed * Time.fixedDeltaTime, body.velocity.y);

        if (sight.CallSightCheck())
        {
            animator.SetTrigger("Detect");
            behaviorState = EnemyState.Detect;
            inspectionTarget = player.transform.position;
        }
    }

    private void ToggleDirection(float target)
    {
        if (lookDirection == Direction.Right && target > body.position.x)
            return;
        if (lookDirection == Direction.Left && target < body.position.x)
            return;

        body.transform.localScale *= new Vector2(-1f, 1f);

        lookDirection = (lookDirection == Direction.Right)
                    ? Direction.Left
                    : Direction.Right; //switches from left to right and vice versa

        sight.SetDirection(lookDirection);
    }

    private IEnumerator ResolvePatrolPoint()
    {
        canMove = false;
        animator.SetBool("Move", false);

        yield return new WaitForSeconds(patrolPoints[nextPatrolIndex].WaitTime);

        if (behaviorState != EnemyState.Patrol) yield break;

        canMove = true;
        animator.SetBool("Move", true);

        nextPatrolIndex = (nextPatrolIndex + 1) % patrolPoints.Length; //cycles back to 0 if it goes over the limit

        nextPatrolPoint = new Vector2(patrolPoints[nextPatrolIndex].transform.position.x, body.position.y);

        if (patrolPoints[nextPatrolIndex].TurnAround) ToggleDirection(nextPatrolPoint.x);
    }

    public void CallCheckState()
    {
        behaviorState = EnemyState.Check;
    }

    private void Check()
    {
        if (sight.CallSightCheck())
        {
            animator.SetTrigger("Pursue");
            behaviorState = EnemyState.Pursue;
            return;
        }

        body.velocity = new Vector2(Mathf.Sign(inspectionTarget.x - body.position.x) * checkSpeed * Time.fixedDeltaTime, body.velocity.y);

        if (transform.position.x >= inspectionTarget.x - 1f
            && transform.position.x <= inspectionTarget.x + 1f)
        {
            startAlertTime = Time.time;
            animator.SetTrigger("Search");
            behaviorState = EnemyState.Search;
        }
    }

    private void Search()
    {
        if (sight.CallSightCheck())
        {
            animator.SetTrigger("Pursue");
            behaviorState = EnemyState.Pursue;
            return;
        }

        if (Time.time - startAlertTime >= alertTime)
        {
            nextPatrolIndex = 0;
            nextPatrolPoint = new Vector2(patrolPoints[nextPatrolIndex].transform.position.x, body.position.y);
            ToggleDirection(nextPatrolPoint.x);
            canMove = true;
            animator.SetTrigger("Return");
            behaviorState = EnemyState.Patrol;
        }
    }

    private void Pursue()
    {
        if (canMove)
            body.velocity = new Vector2(Mathf.Sign(player.transform.position.x - body.position.x) * pursueSpeed * Time.fixedDeltaTime, body.velocity.y);

        if (!sight.CallSightCheck())
        {
            if (startPursueTime == 0f)
            {
                inspectionTarget = new Vector2(player.transform.position.x, body.position.y);
                startPursueTime = Time.time;
                return;
            }

            if (Time.time - startPursueTime < pursueTime)
                return;

            if (body.position.x >= inspectionTarget.x - 0.1f
                && body.position.x <= inspectionTarget.x + 0.1f)
            {
                startPursueTime = 0f;
                startAlertTime = Time.time;
                behaviorState = EnemyState.Search;
                animator.SetTrigger("Search");
                sight.SetPursuit(false);
                return;
            }


            startPursueTime = 0f;
            behaviorState = EnemyState.Check;
            animator.SetTrigger("LostSight");
            sight.SetPursuit(false);
            return;
        }

        ToggleDirection(player.transform.position.x);

        if (body.position.x <= player.transform.position.x + attackRange
            && body.position.x >= player.transform.position.x - attackRange)
        {
            behaviorState = EnemyState.Attack;
            animator.SetTrigger("StartAttack");
            Attack();
            return;
        }

        inspectionTarget = new Vector2(player.transform.position.x, body.position.y);

        sight.SetPursuit(true);
    }

    private void Attack()
    {
        sight.CallSightCheck();
        ToggleDirection(player.transform.position.x);

        if (body.position.x > player.transform.position.x + attackRange ||
            body.position.x < player.transform.position.x - attackRange)
        {
            animator.SetTrigger("Pursue");
            behaviorState = EnemyState.Pursue;
            return;
        }

        if (!canAttack)
            return;

        animator.SetTrigger("Attack");
        StartCoroutine(AttackCooldown());
    }

    private IEnumerator AttackCooldown()
    {
        canAttack = false;

        yield return new WaitForSeconds(attackCooldown);

        canAttack = true;
    }

    private void DealDamage()
    {

    }

    private void ToggleMovementOn()
    {
        canMove = true;
    }

    private void ToggleMovementOff()
    {
        canMove = false;
    }
}

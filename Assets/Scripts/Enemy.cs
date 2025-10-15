using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("REFERENCES")]
    [Space]
    [SerializeField] private Transform wallCheck;
    [SerializeField] private LayerMask whatIsWall;
    [SerializeField] private GameObject inspectionIndicator;
    [SerializeField, Space] private PatrolPoint[] patrolPoints;

    [Space]
    [Header("BEHAVIOR")]
    [Space]
    [SerializeField] private float maxAlertTime;
    [SerializeField] private float maxPursueTime;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackCooldown;
    [SerializeField] private EnemyState behaviorState = EnemyState.Patrol;

    [Space]
    [Header("MOVEMENT")]
    [Space]
    [SerializeField] private float patrolSpeed;
    [SerializeField] private float checkSpeed;
    [SerializeField] private float pursueSpeed;
    [SerializeField] private float jumpForce;


    private Rigidbody2D body;
    private Animator animator;
    private EnemySight sight;
    private GameObject player;
    private Direction lookDirection = Direction.Right;
    private Vector2 nextPatrolPoint;
    private Vector2 inspectionTarget;
    private float startPursueTime = 0f;
    private float startAlertTime;
    private int nextPatrolIndex = 0;
    private bool canMove = true;
    private bool canAttack = true;
    private bool canJump = true;
    private bool isPlayerInSight;
    private float internalClock;
    private const float tickCooldown = 0.0f;

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
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sight = transform.parent.GetComponent<EnemySight>();
        player = GameObject.FindWithTag("Player");
        nextPatrolPoint = new Vector2(patrolPoints[0].transform.position.x, body.position.y);
        internalClock = Time.time;

        if (behaviorState == EnemyState.Patrol) animator.SetBool("Move", true);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == patrolPoints[nextPatrolIndex].gameObject && behaviorState == EnemyState.Patrol)
        {
            StartCoroutine(ResolvePatrolPoint());
        }
    }

    private void Update()
    {
        if (Time.time - internalClock >= tickCooldown)
        {
            internalClock = Time.time;
            isPlayerInSight = sight.CallSightCheck();
        }
    }

    private void FixedUpdate()
    {
        CheckWall();

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

        if (isPlayerInSight)
        {
            sight.SetAlert(true, inspectionTarget);
            animator.SetTrigger("Detect");
            behaviorState = EnemyState.Detect;
            CreateInspectionPoint();
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
        if (isPlayerInSight)
        {
            animator.SetTrigger("Pursue");
            behaviorState = EnemyState.Pursue;
            sight.SetAlert(false, inspectionTarget);
            return;
        }

        body.velocity = new Vector2(Mathf.Sign(inspectionTarget.x - body.position.x) * checkSpeed * Time.fixedDeltaTime, body.velocity.y);

        if (IsWithinDistance(inspectionTarget, 1f))
        {
            startAlertTime = Time.time;
            sight.SetAlert(true, inspectionTarget);
            animator.SetTrigger("Search");
            behaviorState = EnemyState.Search;
        }
    }

    private void Search()
    {
        if (isPlayerInSight)
        {
            sight.SetAlert(true, inspectionTarget);
            animator.SetTrigger("Pursue");
            behaviorState = EnemyState.Pursue;
            return;
        }

        if (Time.time - startAlertTime >= maxAlertTime)
        {
            nextPatrolIndex = 0;
            nextPatrolPoint = new Vector2(patrolPoints[nextPatrolIndex].transform.position.x, body.position.y);
            ToggleDirection(nextPatrolPoint.x);
            canMove = true;
            sight.SetAlert(false, inspectionTarget);
            animator.SetTrigger("Return");
            behaviorState = EnemyState.Patrol;
        }
    }

    private void Pursue()
    {
        sight.SetAlert(true, inspectionTarget);

        if (canMove)
        {
            body.velocity = new Vector2(Mathf.Sign(player.transform.position.x - body.position.x) * pursueSpeed * Time.fixedDeltaTime, body.velocity.y);
            ToggleDirection(player.transform.position.x);
        }

        if (!isPlayerInSight)
        {

            if (startPursueTime == 0f)
            {
                CreateInspectionPoint();
                startPursueTime = Time.time;
                return;
            }

            if (Time.time - startPursueTime < maxPursueTime)
                return;

            if (IsWithinDistance(inspectionTarget, 1f))
            {
                startPursueTime = 0f;
                startAlertTime = Time.time;
                sight.SetAlert(true, inspectionTarget);
                animator.SetTrigger("Search");
                behaviorState = EnemyState.Search;
                return;
            }


            startPursueTime = 0f;
            sight.SetAlert(true, inspectionTarget);
            animator.SetTrigger("LostSight");
            behaviorState = EnemyState.Check;
            return;
        }

        if (IsWithinAttackRange())
        {
            sight.SetAlert(true, inspectionTarget);
            animator.SetTrigger("StartAttack");
            behaviorState = EnemyState.Attack;
            Attack();
            return;
        }

        CreateInspectionPoint();
    }

    private void Attack()
    {
        if (canMove)
            ToggleDirection(player.transform.position.x);

        if (!IsWithinAttackRange())
        {
            sight.SetAlert(true, inspectionTarget);
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

    private void CheckWall()
    {
        if (!canJump) return;

        if (Physics2D.OverlapCircle(wallCheck.position, .2f, whatIsWall))
        {
            body.AddForce(new Vector2(0f, jumpForce));

            StartCoroutine(JumpCooldown());
        }
    }

    private IEnumerator JumpCooldown()
    {
        canJump = false;

        yield return new WaitForSeconds(2f);

        canJump = true;
    }

    private bool IsWithinDistance(Vector2 target, float range)
    {
        Vector2 toTarget = new Vector2(target.x, body.position.y) - body.position;
        return toTarget.sqrMagnitude <= range * range;
    }

    private bool IsWithinAttackRange()
    {
        Vector2 toTarget = (Vector2)player.transform.position - body.position;
        return toTarget.sqrMagnitude <= attackRange * attackRange;
    }

    private void CreateInspectionPoint()
    {
        Vector2 lastSeenPos = (Vector2)player.transform.position + new Vector2(0f, 0.5f);

        RaycastHit2D hit = Physics2D.Raycast(lastSeenPos, Vector2.down, Mathf.Infinity, whatIsWall);
        if (hit.collider != null) lastSeenPos = hit.point + new Vector2(0f, 0.5f);

        inspectionTarget = lastSeenPos;
        GameObject spot = Instantiate(inspectionIndicator, inspectionTarget, Quaternion.identity);
        Destroy(spot, 3f);
    }
}

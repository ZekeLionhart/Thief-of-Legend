using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("REFERENCES")]
    [Space]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.01f;
    [SerializeField] private LayerMask groundMask;
    [SerializeField] private GameObject inspectionIndicator;
    [SerializeField, Space] private PatrolPoint[] patrolPoints;

    [Space]
    [Header("BEHAVIOR")]
    [Space]
    [SerializeField] private float maxAlertTime;
    [SerializeField] private float maxPursueTime;
    [SerializeField] private float attackRange;
    [SerializeField] private float attackCooldown;
    [SerializeField] private EnemyState behaviorState = EnemyState.PatrolMove;

    [Space]
    [Header("MOVEMENT")]
    [Space]
    [SerializeField] private float patrolSpeed;
    [SerializeField] private float checkSpeed;
    [SerializeField] private float pursueSpeed;
    [SerializeField] private float jumpClearance = 0.5f;
    [SerializeField] private float landingOffset = 0.5f;
    [SerializeField] private float maxJumpHeight = 3.5f;
    [SerializeField] private float maxJumpHorizontalSpeed = 6f;

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
    private bool isJumping;
    private bool isPlayerInSight;
    private float patrolWaitEndTime;
    private float attackCooldownEndTime;
    private float internalClock;
    private const float tickCooldown = 0.0f;

    public enum Direction
    {
        Left,
        Right
    }

    public enum EnemyState
    {
        PatrolMove,
        PatrolIdle,
        Detect,
        Check,
        Search,
        Pursue,
        Attack,
        AttackCooldown
    }

    private void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        sight = transform.parent.GetComponent<EnemySight>();
        player = GameObject.FindWithTag("Player");
        nextPatrolPoint = new(patrolPoints[0].transform.position.x, body.position.y);
        internalClock = Time.time;

        if (behaviorState == EnemyState.PatrolMove) animator.SetBool("Move", true);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isJumping) return;

        if (collision.CompareTag("JumpPad"))
        {
            JumpPad jumpPad = collision.GetComponent<JumpPad>();
            Jump(jumpPad.GetLandingPad);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isJumping || behaviorState != EnemyState.PatrolMove)
            return;

        if (collision.gameObject != patrolPoints[nextPatrolIndex].gameObject)
            return;

        if (!Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask))
            return;

        body.velocity = new Vector2(0f, body.velocity.y);
        animator.SetBool("Move", false);

        patrolWaitEndTime = Time.time + patrolPoints[nextPatrolIndex].WaitTime;

        behaviorState = EnemyState.PatrolIdle;
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        if (!isJumping || body.velocity.y > 0f) return;

        if ((groundMask.value & (1 << collision.gameObject.layer)) == 0) return;

        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > 0.5f)
            {
                isJumping = false;
                break;
            }
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
        if (isJumping) return;

        switch (behaviorState)
        {
            case EnemyState.PatrolMove:
                PatrolMove();
                break;
            case EnemyState.PatrolIdle:
                PatrolIdle();
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
                //Called by the animation
                break;
            case EnemyState.AttackCooldown:
                AttackCooldown();
                break;
        }
    }

    private void PatrolMove()
    {
        Move(nextPatrolPoint, patrolSpeed);
        CheckForPlayer();
    }

    private void PatrolIdle()
    {
        CheckForPlayer();

        if (behaviorState != EnemyState.PatrolIdle)
            return;

        if (Time.time < patrolWaitEndTime)
            return;

        nextPatrolIndex = (nextPatrolIndex + 1) % patrolPoints.Length;

        nextPatrolPoint = new Vector2(
            patrolPoints[nextPatrolIndex].transform.position.x,
            body.position.y);

        if (patrolPoints[nextPatrolIndex].TurnAround)
            ToggleDirection(nextPatrolPoint.x);

        animator.SetBool("Move", true);
        behaviorState = EnemyState.PatrolMove;
    }

    private void CheckForPlayer()
    {
        if (!isPlayerInSight)
            return;

        body.velocity = new Vector2(0f, body.velocity.y);
        CreateInspectionPoint();
        sight.SetAlert(true, inspectionTarget);
        animator.SetTrigger("Detect");
        behaviorState = EnemyState.Detect;
    }

    private void ToggleDirection(float target)
    {
        if (lookDirection == Direction.Right && target > body.position.x)
            return;
        if (lookDirection == Direction.Left &&  target < body.position.x)
            return;

        body.transform.localScale *= new Vector2(-1f, 1f);

        lookDirection = (lookDirection == Direction.Right)
                    ? Direction.Left
                    : Direction.Right; //switches from left to right and vice versa
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

        Move(inspectionTarget, checkSpeed);

        if (IsWithinDistance(inspectionTarget, 1f))
        {
            body.velocity = new(0f, body.velocity.y);
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
            animator.SetBool("Move", true);
            animator.SetTrigger("Return");
            sight.SetAlert(false, inspectionTarget);
            behaviorState = EnemyState.PatrolMove;
        }
    }

    private void Pursue()
    {
        if (isPlayerInSight)
        {
            CreateInspectionPoint();
            startPursueTime = 0f;
        }

        sight.SetAlert(true, inspectionTarget);

        Move(inspectionTarget, pursueSpeed);
        ToggleDirection(inspectionTarget.x);

        if (!isPlayerInSight)
        {

            if (startPursueTime == 0f)
            {
                startPursueTime = Time.time;
                return;
            }

            if (Time.time - startPursueTime < maxPursueTime)
                return;

            if (IsWithinDistance(inspectionTarget, 1f))
            {
                body.velocity = new(0f, body.velocity.y);
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
            BeginAttack();
            return;
        }
    }

    private void Move(Vector2 destination, float speed)
    {
        if (isJumping) return;

        body.velocity = new Vector2(
            Mathf.Sign(destination.x - body.position.x) * speed * Time.fixedDeltaTime,
            body.velocity.y);
    }

    private void BeginAttack()
    {
        body.velocity = new Vector2(0f, body.velocity.y);
        behaviorState = EnemyState.Attack;
        animator.SetTrigger("Attack");
    }

    public void EnterAttackCooldown()
    {
        if (behaviorState != EnemyState.Attack) return;

        body.velocity = new Vector2(0f, body.velocity.y);

        attackCooldownEndTime = Time.time + attackCooldown;
        behaviorState = EnemyState.AttackCooldown;
    }

    private void AttackCooldown()
    {
        ToggleDirection(player.transform.position.x);

        if (!IsWithinAttackRange())
        {
            sight.SetAlert(true, inspectionTarget);
            animator.SetTrigger("Pursue");
            behaviorState = EnemyState.Pursue;
            return;
        }

        if (Time.time >= attackCooldownEndTime)
            BeginAttack();
    }

    private void DealDamage()
    {

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

        RaycastHit2D hit = Physics2D.Raycast(lastSeenPos, Vector2.down, Mathf.Infinity, groundMask);
        if (hit.collider != null) lastSeenPos = hit.point + new Vector2(0f, 0.5f);

        inspectionTarget = lastSeenPos;
        GameObject spot = Instantiate(inspectionIndicator, inspectionTarget, Quaternion.identity);
        Destroy(spot, 3f);
    }

    public void Jump(Transform jumpTarget)
    {
        if (!Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundMask)
            || isJumping || jumpTarget == null)
            return;

        bool movingState = behaviorState == EnemyState.PatrolMove
            || behaviorState == EnemyState.Check
            || behaviorState == EnemyState.Pursue;

        if (!movingState) return;

        Vector2 destination = (Vector2)jumpTarget.position + Vector2.up * landingOffset;
        float requiredRise = Mathf.Max(0f, destination.y - body.position.y) + jumpClearance;
        
        if (requiredRise > maxJumpHeight) return;
        
        Vector2 launchVelocity = CalculateJumpVelocity(destination);
        Debug.Log(launchVelocity.x + " vs " + maxJumpHorizontalSpeed);
        if (Mathf.Abs(launchVelocity.x) > maxJumpHorizontalSpeed) return;

        ToggleDirection(destination.x);

        isJumping = true;
        body.velocity = CalculateJumpVelocity(destination);
    }

    private Vector2 CalculateJumpVelocity(Vector2 destination)
    {
        float gravity = -(Physics2D.gravity.y * body.gravityScale);

        float apexY = Mathf.Max(body.position.y, destination.y) + jumpClearance;

        float riseHeight = apexY - body.position.y;
        float fallHeight = apexY - destination.y;

        float verticalSpeed = Mathf.Sqrt(2f * gravity * riseHeight);

        float riseTime = verticalSpeed / gravity;
        float fallTime = Mathf.Sqrt(2f * fallHeight / gravity);
        float flightTime = riseTime + fallTime;

        float horizontalSpeed = (destination.x - body.position.x) / flightTime;

        return new Vector2(horizontalSpeed, verticalSpeed);
    }
}

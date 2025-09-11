using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private EnemySight sight;
    [SerializeField, InspectorName("Starting Behavior")] private EnemyState behaviorState = EnemyState.Patrol;
    [SerializeField] private float patrolSpeed;
    [SerializeField] private PatrolPoint[] patrolPoints;
    private Vector2 nextPatrolPoint;
    private int nextPatrolIndex = 0;
    private bool canMove = true;
    private Direction patrolDirection = Direction.Right;
    private GameObject player;

    public enum Direction
    {
        Left,
        Right
    }

    public enum EnemyState
    {
        Patrol,
        Attack,
        Pursue,
        Search
    }

    private void Awake()
    {
        nextPatrolPoint = new Vector2(patrolPoints[0].transform.position.x, body.position.y);
        player = GameObject.Find("Player");

        if (behaviorState == EnemyState.Patrol) animator.SetTrigger("Move");
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
            case EnemyState.Attack:
                Attack();
                break;
            case EnemyState.Pursue:
                PursueTarget();
                break;
            case EnemyState.Search:
                SearchForTarget();
                break;
        }
    }

    private void Patrol()
    {
        if (canMove)
            body.MovePosition(Vector2.MoveTowards(body.position, nextPatrolPoint, patrolSpeed * Time.fixedDeltaTime));

        if (sight.CallSightCheck()) { animator.SetTrigger("Detect"); behaviorState = EnemyState.Pursue; }
    }

    private void ToggleDirection()
    {
        body.transform.localScale *= new Vector2(-1f, 1f);

        patrolDirection = (patrolDirection == Direction.Right)
                    ? Direction.Left
                    : Direction.Right; //switches from left to right and vice versa

        sight.SetDirection(patrolDirection);
    }

    private IEnumerator ResolvePatrolPoint()
    {
        canMove = false;
        animator.SetBool("Move", false);

        yield return new WaitForSeconds(patrolPoints[nextPatrolIndex].WaitTime);

        canMove = true;
        animator.SetBool("Move", true);

        if (patrolPoints[nextPatrolIndex].TurnAround) ToggleDirection();

        nextPatrolIndex = (nextPatrolIndex + 1) % patrolPoints.Length; //cycles back to 0 if it goes over the limit

        nextPatrolPoint = new Vector2(patrolPoints[nextPatrolIndex].transform.position.x, body.position.y);
    }

    private void PursueTarget()
    {
        if (!sight.CallSightCheck())
        {
            behaviorState = EnemyState.Patrol;
            return;
        }

        if (transform.position.x <= player.transform.position.x + 1
            && transform.position.x >= player.transform.position.x - 1)
        {
            animator.SetBool("Move", false);
            canMove = false;

            animator.SetBool("Attack", true);

            return;
        }
        if (canMove)
            body.MovePosition(Vector2.MoveTowards(body.position, player.transform.position, patrolSpeed * Time.fixedDeltaTime));
    }

    private void SearchForTarget()
    {

    }

    private void Attack()
    {

    }

    private void StartMove() { }

    private IEnumerator AttackCooldown() 
    {
        canMove = false;
        animator.SetBool("Attack", false);
        yield return 2f;

        if (transform.position.x <= player.transform.position.x + 1
            && transform.position.x >= player.transform.position.x - 1)
            animator.SetBool("Attack", true);
        else
        {
            animator.SetBool("Move", true);
            canMove = true;
        }
    }
}

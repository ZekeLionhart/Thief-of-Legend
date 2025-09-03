using System.Collections;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    [SerializeField] private Rigidbody2D body;
    [SerializeField] private Collider2D bodyCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private EnemySight sight;
    [SerializeField, InspectorName("Starting Behavior")] private EnemyState behaviorState = EnemyState.Patroling;
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
        Patroling,
        Alert,
        Pursuing,
        Searching
    }

    private void Awake()
    {
        nextPatrolPoint = new Vector2(patrolPoints[0].transform.position.x, body.position.y);
        player = GameObject.Find("Player");
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == patrolPoints[nextPatrolIndex].gameObject && behaviorState == EnemyState.Patroling)
        {
            StartCoroutine(ResolvePatrolPoint());
        }
    }

    private void FixedUpdate()
    {
        switch (behaviorState)
        {
            case EnemyState.Patroling:
                Patrol();
                break;
            case EnemyState.Alert:
                LookAround();
                break;
            case EnemyState.Pursuing:
                PursueTarget();
                break;
            case EnemyState.Searching:
                SearchForTarget();
                break;
        }
    }

    private void Patrol()
    {
        if (canMove)
            body.MovePosition(Vector2.MoveTowards(body.position, nextPatrolPoint, patrolSpeed * Time.fixedDeltaTime));

        if (sight.CallSightCheck()) behaviorState = EnemyState.Pursuing;
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
        animator.SetBool("IsColliding", true);

        yield return new WaitForSeconds(patrolPoints[nextPatrolIndex].WaitTime);

        canMove = true;
        animator.SetBool("IsColliding", false);

        if (patrolPoints[nextPatrolIndex].TurnAround) ToggleDirection();

        nextPatrolIndex = (nextPatrolIndex + 1) % patrolPoints.Length; //cycles back to 0 if it goes over the limit

        nextPatrolPoint = new Vector2(patrolPoints[nextPatrolIndex].transform.position.x, body.position.y);
    }

    private void PursueTarget()
    {
        if (!sight.CallSightCheck())
        {
            behaviorState = EnemyState.Patroling;
            return;
        }

        if (transform.position.x <= player.transform.position.x + 1
            && transform.position.x >= player.transform.position.x - 1)
        {
            animator.SetBool("IsColliding", true);
            //animator.SetTrigger("OnAttackCldwn");
            return;
        }

        animator.SetBool("IsColliding", false);
        body.MovePosition(Vector2.MoveTowards(body.position, player.transform.position, patrolSpeed * Time.fixedDeltaTime));
    }

    private void SearchForTarget()
    {

    }

    private void LookAround()
    {

    }

    private void StartMove() { }

    private IEnumerator AttackCooldown() 
    {
        //canMove = false;
        //canAttack = false;
        yield return 1f;

        animator.SetTrigger("OnAttackCldwn");
        //canAttack = true;
    }

    private void Attack() { }
}

using UnityEngine;
using UnityEngine.AI;

public class ActiveAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    public float patrolRadius = 10f;
    public float patrolWaitTime = 2f;

    [Header("Detection Settings")]
    public Transform player;
    public float detectionRadius = 5f;
    public float stopDistanceFromPlayer = 2f;

    [Header("Movement")]
    public NavMeshAgent agent;

    [Header("Animation")]
    public Animator animator;
    public string noticeTrigger = "Notice";
    public string isMovingBool = "IsMoving";

    private Vector3 patrolTarget;
    private bool isChasing = false;
    private bool hasNoticed = false;
    private float patrolTimer = 0f;

    void Start()
    {
        if (agent == null)
            agent = GetComponent<NavMeshAgent>();

        if (animator == null)
            animator = GetComponent<Animator>();

        SetNewPatrolTarget();
    }

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= detectionRadius)
        {
            if (!hasNoticed)
            {
                hasNoticed = true;
                animator.SetTrigger(noticeTrigger);
            }

            isChasing = true;

            if (distanceToPlayer > stopDistanceFromPlayer)
            {
                agent.SetDestination(player.position);
                SetMoving(true);
            }
            else
            {
                agent.ResetPath();
                SetMoving(false);
            }
        }
        else
        {
            if (isChasing)
            {
                // Optional: add a delay or look-around animation here when losing sight
            }

            isChasing = false;
            hasNoticed = false;
            Patrol();
        }
    }

    void Patrol()
    {
        if (agent.remainingDistance <= agent.stoppingDistance && !agent.pathPending)
        {
            patrolTimer += Time.deltaTime;
            SetMoving(false);

            if (patrolTimer >= patrolWaitTime)
            {
                SetNewPatrolTarget();
                patrolTimer = 0f;
            }
        }
        else
        {
            SetMoving(true);
        }
    }

    void SetNewPatrolTarget()
    {
        Vector3 randomDirection = Random.insideUnitSphere * patrolRadius;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, patrolRadius, NavMesh.AllAreas))
        {
            patrolTarget = hit.position;
            agent.SetDestination(patrolTarget);
        }
    }

    void SetMoving(bool isMoving)
    {
        if (animator != null)
            animator.SetBool(isMovingBool, isMoving);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, patrolRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, stopDistanceFromPlayer);
    }
}

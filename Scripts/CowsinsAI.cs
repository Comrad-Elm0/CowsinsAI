using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class CowsinsAI : MonoBehaviour
{
    #region Generic Variables

    [Tooltip("WILL NOT SAVE CHANGES WHEN ENTERING PLAY")] public States currentState;
    [Tooltip("Whether the AI should attack the player")] public bool dumbAI = true;
    [Tooltip("How long the AI should wait until going to the next waypoint")] public float waitTime = 1f;
    [Tooltip("If the AI should use waypoints or randomly move about")] public bool useWaypoints;
    public Transform[] waypoints;
    [Tooltip("The animator that the AI will use for shooter")] public Animator shooterAnimator;
    public enum States
    {
        Idle,
        Attack,
        Search
    }

    int currentWaypoint = 0;
    float waitTimer = 0f;
    public bool useRagdoll;
    NavMeshAgent agent;

    #endregion

    #region Wander AI Variables

    Vector3 destination;
    bool wandering = false;
    float waitTimeBetweenWander = 2f;
    float wanderTimer = 0f;

    [Tooltip("How far the AI should wander until changing path")] public float wanderRadius = 10f;
    [Tooltip("Minimum amount of steps it should do until moving again")] public float minWanderDistance = 2f;
    [Tooltip("Maximum amount of steps it should do until moving again")] public float maxWanderDistance = 5f;

    #endregion

    #region Location Variables

    [Header("Player Searching Variables")]
    [Tooltip("The radius in what the AI can see")] public float searchRadius;
    [Range(0, 360)]
    [Tooltip("The FOV of what the AI can see")] public float searchAngle;

    [HideInInspector] public GameObject player;

    [Tooltip("The layer in which the AI will shoot at")] public LayerMask targetMask;
    [Tooltip("The layer in which the AI cannot see through")] public LayerMask obstructionMask;
    [Tooltip("Debug variable, changing will not make any difference")] public bool canSeePlayer;

    [Tooltip("How long the AI will spend trying to find the player after losing sight")] public float waitTimeToSearch;

    float searchTimer = 5;
    float currentSearchTime;
    bool searchTimerStarted = false;

    #region DebugBools
    public bool shootingDistanceDebug;
    public bool meleeDistanceDebug;
    public bool canSeePlayerDebug;

    public bool searchRadiusDebug;
    public bool attackRadiusDebug;
    #endregion

    #endregion

    #region Shooter Variables
    [Tooltip("The projectile prefab that the AI will use")]public GameObject projectile;
    [Tooltip("Where the bullet will shoot from")] public Transform firePoint;
    [Tooltip("How far the AI should shoot from")] public float shootDistance;
    public bool inShootingDistance;
    [Tooltip("How long the AI should wait inbetween each shot")] public float timeBetweenAttacks;
    bool alreadyAttackedShooter;
    bool alreadyAttackedMelee;
    #endregion

    #region Melee Variables
    [Tooltip("The animator that the AI will use for melee")] public Animator meleeAnimator;
    [Tooltip("How long the AI will wait inbetween attacks")] public float waitBetweenAttack;
    public float waitBetweenSwingDelay;
    [Tooltip("How far the AI will stand from the player whilst attacking")] public float meleeDistance;
    public bool inMeleeDistance;
    #endregion

    #region Debug Variables
    public AudioSource gunShotSfx;

    public bool melee;
    public bool shooter;
    #endregion

    public void Start()
    {
        currentState = States.Idle;
        agent = gameObject.GetComponentInChildren<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player");
        StartCoroutine(FOVRoutine());
    }

    void Update()
    {
        if (currentState == States.Idle)
        {
            IdleHostileState();
        }
        else if (currentState == States.Search)
        {
            SearchHostileState();
        }
        else if (currentState == States.Attack)
        {
            AttackState();
        }
    }

    void IdleHostileState()
    {
        if (!dumbAI)
        {
            agent.isStopped = false;

            if (useWaypoints == true)
            {
                Waypoints();
            }
            else
            {
                RandomMove();
            }

            FieldOfViewCheck();

            if (canSeePlayer == true)
            {
                currentState = States.Attack;
            }

            if (shooter == true)
            {
                if (agent.velocity != Vector3.zero)
                {
                    shooterAnimator.SetBool("isWalking", true);
                }
                else if (agent.velocity == Vector3.zero)
                {
                    shooterAnimator.SetBool("isWalking", false);
                }
            }

            if (melee == true)
            {
                if (agent.velocity != Vector3.zero)
                {
                    meleeAnimator.SetBool("isWalking", true);
                }
                else if (agent.velocity == Vector3.zero)
                {
                    meleeAnimator.SetBool("isWalking", false);
                }
            }
        }
    }

    void SearchHostileState()
    {
        agent.isStopped = false;

        RandomMove();

        if (!searchTimerStarted)
        {
            currentSearchTime = searchTimer;
            searchTimerStarted = true;
        }

        if (agent.velocity != Vector3.zero)
        {
            shooterAnimator.SetBool("combatWalk", true);

            if (shooterAnimator.GetBool("isWalking"))
            {
                shooterAnimator.SetBool("isWalking", false);
            }

            shooterAnimator.SetBool("combatIdle", false);
        }
        else if (agent.velocity == Vector3.zero)
        {
            shooterAnimator.SetBool("combatWalk", false);
            shooterAnimator.SetBool("combatIdle", true);
        }

        currentSearchTime -= Time.deltaTime;

        if (currentSearchTime <= 0)
        {
            currentState = States.Idle;

            shooterAnimator.SetBool("combatIdle", false);
            shooterAnimator.SetBool("combatWalk", false);

            shooterAnimator.SetBool("isWalking", false);
        }

        if (canSeePlayer == true)
        {
            currentState = States.Attack;
        }
    }

    #region Field Of View Functions

    private IEnumerator FOVRoutine()
    {
        WaitForSeconds wait = new WaitForSeconds(0.2f);

        while (true)
        {
            yield return wait;
            FieldOfViewCheck();
        }
    }

    void FieldOfViewCheck()
    {
        Collider[] rangeChecks = Physics.OverlapSphere(transform.position, searchRadius, targetMask);

        if (rangeChecks.Length != 0)
        {
            Transform target = rangeChecks[0].transform;
            Vector3 directionToTarget = (target.position - transform.position).normalized;

            if (Vector3.Angle(transform.forward, directionToTarget) < searchAngle / 2)
            {
                float distanceToTarget = Vector3.Distance(transform.position, target.position);

                if (!Physics.Raycast(transform.position, directionToTarget, distanceToTarget, obstructionMask))
                {
                    canSeePlayer = true;
                }
                else
                {
                    canSeePlayer = false;
                }
            }
            else
            {
                canSeePlayer = false;
            }
        }
        else if (canSeePlayer)
        {
            canSeePlayer = false;
        }
    }

    #endregion

    void Waypoints()
    {
        if (agent.remainingDistance < 0.5f)
        {
            waitTimer += Time.deltaTime;
            if (waitTimer >= waitTime)
            {
                currentWaypoint++;
                if (currentWaypoint >= waypoints.Length)
                {
                    currentWaypoint = 0;
                }
                agent.destination = waypoints[currentWaypoint].position;
                waitTimer = 0f;
            }
        }
    }

    #region Wander Mechanics
    void RandomMove()
    {
        if (!wandering)
        {
            if (wanderTimer <= 0f)
            {
                destination = RandomNavSphere(transform.position, wanderRadius, -1);
                agent.SetDestination(destination);
                wandering = true;
                wanderTimer = waitTimeBetweenWander;
            }
            else
            {
                wanderTimer -= Time.deltaTime;
            }
        }
        else if (agent.remainingDistance <= minWanderDistance)
        {
            wandering = false;
        }

    }

    public static Vector3 RandomNavSphere(Vector3 origin, float dist, int layerMask)
    {
        Vector3 randDirection = Random.insideUnitSphere * dist;
        randDirection += origin;

        NavMeshHit navHit;
        NavMesh.SamplePosition(randDirection, out navHit, dist, layerMask);

        return navHit.position;
    }

    #endregion

    #region Attack States
    void AttackState()
    {
        if (shooter == true)
        {
            ShooterAttack();
        }

        else if (melee == true)
        {
            MeleeAttack();
        }

        if (canSeePlayer == false)
        {
            currentState = States.Search;
        }

    }

    // Shooter Functions
    void ResetAttackShooter()
    {
        alreadyAttackedShooter = false;

        shooterAnimator.SetBool("firing", false);
    }
    
    void ShooterAttack()
    {
        if (!dumbAI)
        {
            if (agent.velocity != Vector3.zero)
            {
                shooterAnimator.SetBool("combatWalk", true);
                shooterAnimator.SetBool("isWalking", false);
                shooterAnimator.SetBool("combatIdle", false);
            }
            else if (agent.velocity == Vector3.zero)
            {
                shooterAnimator.SetBool("combatWalk", false);
                shooterAnimator.SetBool("combatIdle", true);
            }

            agent.destination = player.transform.position;
        }
        
        float distanceToPlayer = Vector3.Distance(player.transform.position, agent.transform.position);

        if (distanceToPlayer <= shootDistance)
        {
            inShootingDistance = true;
            if (!dumbAI)
            {
                agent.SetDestination(transform.position);
            }
            transform.LookAt(new Vector3(player.transform.position.x, transform.position.y, player.transform.position.z));
            if (!alreadyAttackedShooter)
            {
                Rigidbody rb = Instantiate(projectile, firePoint.transform.position, Quaternion.identity).GetComponent<Rigidbody>();
                rb.AddForce(transform.forward * 32f, ForceMode.Impulse);
                rb.AddForce(transform.up * 2f, ForceMode.Impulse);
                alreadyAttackedShooter = true;
                Invoke(nameof(ResetAttackShooter), timeBetweenAttacks);
                shooterAnimator.SetBool("firing", true);
            }
        }
        else if (distanceToPlayer >= shootDistance)
        {
            inShootingDistance = false;
            if (!dumbAI)
            {
                agent.isStopped = false;
            }
            shooterAnimator.SetBool("firing", false);
        }
    }

    // Melee Functions
    void ResetAttackMelee()
    {
        alreadyAttackedMelee = false;

        meleeAnimator.SetBool("attacking", false);
    }

    void MeleeAttack()
    {
        if (agent.velocity != Vector3.zero)
        {
            meleeAnimator.SetBool("isWalking", true);
        }
        else if (agent.velocity == Vector3.zero)
        {
            meleeAnimator.SetBool("isWalking", false);
        }

        float distanceToPlayer = Vector3.Distance(player.transform.position, agent.transform.position);

        agent.destination = player.transform.position;

        if (distanceToPlayer <= meleeDistance)
        {
            inMeleeDistance = true;
            agent.isStopped = true;

            if (agent.velocity != Vector3.zero)
            {
                meleeAnimator.SetBool("isWalking", true);
            }
            else if (agent.velocity == Vector3.zero)
            {
                meleeAnimator.SetBool("isWalking", false);
            }

            if (!alreadyAttackedMelee)
            {
                alreadyAttackedMelee = true;

                meleeAnimator.SetBool("attacking", true);
                Invoke(nameof(ResetAttackMelee), waitBetweenAttack);
            }
        }
        else if (distanceToPlayer >= meleeDistance)
        {
            inMeleeDistance = false;
            agent.isStopped = false;

            meleeAnimator.SetBool("attacking", false);
        }
    }

    

    #endregion
}
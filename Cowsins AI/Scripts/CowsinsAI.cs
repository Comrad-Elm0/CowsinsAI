using System.Collections;
using UnityEngine;
using UnityEngine.AI;

namespace cowsins.ai
{
    [SelectionBase]
    public class CowsinsAI : MonoBehaviour
    {
        #region Generic Variables

        public States currentState;
        public float waitTime = 1f;
        public bool useWaypoints;
        public Transform[] waypoints;
        public Animator shooterAnimator;
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

        public float wanderRadius = 10f;
        public float minWanderDistance = 2f;
        public float maxWanderDistance = 5f;

        #endregion

        #region Location Variables

        [Header("Player Searching Variables")]
        public float searchRadius;
        [Range(0, 360)]
        public float searchAngle;

        public GameObject player;

        public LayerMask targetMask;
        public LayerMask obstructionMask;
        public bool canSeePlayer;

        public float waitTimeToSearch;

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
        public GameObject projectile;
        public Transform firePoint;
        public float shootDistance;
        public bool inShootingDistance;
        public float timeBetweenAttacks;
        bool alreadyAttackedShooter;
        bool alreadyAttackedMelee;
        #endregion

        #region Melee Variables
        public Animator meleeAnimator;
        public float waitBetweenAttack;
        public float waitBetweenSwingDelay;
        public float meleeDistance;
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
                IdleState();
            }
            else if (currentState == States.Search)
            {
                SearchState();
            }
            else if (currentState == States.Attack)
            {
                AttackState();
            }
        }

        void IdleState()
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

        void SearchState()
        {
            agent.isStopped = true;

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

            float distanceToPlayer = Vector3.Distance(player.transform.position, agent.transform.position);

            agent.destination = player.transform.position;

            if (distanceToPlayer <= shootDistance)
            {
                inShootingDistance = true;
                agent.SetDestination(transform.position);
                transform.LookAt(player.transform);
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
                agent.isStopped = false;
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
}
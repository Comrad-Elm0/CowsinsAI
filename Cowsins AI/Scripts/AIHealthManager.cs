using cowsins.ai;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;

public class AIHealthManager : EnemyAI
{
    bool isDead = false;

    CowsinsAI cai;

    Animator animator;
    NavMeshAgent agent;

    public override void Damage(float damage)
    {
        if (isDead) return;
        base.Damage(damage);
    }

    public override void Die()
    {
        if (isDead) return;
        isDead = true;
        events.OnDeath.Invoke();

        if (shieldSlider != null) shieldSlider.gameObject.SetActive(false);
        if (healthSlider != null) healthSlider.gameObject.SetActive(false);

        if (UI.GetComponent<UIController>().displayEvents)
        {
            UI.GetComponent<UIController>().AddKillfeed(name);
        }

        if(cai.useRagdoll == true)
        {
            Rigidbody[] rigidBodies = gameObject.GetComponentsInChildren<Rigidbody>();

            foreach (Rigidbody rigidBody in rigidBodies)
            {
                rigidBody.isKinematic = false;
            }

            cai.enabled = false;

            animator.enabled = false;

            agent.enabled = false;
        }
        else
        {
            Destroy(gameObject);
        }

        base.Die();
    }

    public override void Update()
    {
        base.Update();
    }

    public override void Start()
    {
        base.Start();

        cai = gameObject.GetComponent<CowsinsAI>();
        if (cai.useRagdoll == true)
        {
            animator = gameObject.GetComponent<Animator>();
            agent = gameObject.GetComponent<NavMeshAgent>();

            Rigidbody[] rigidBodies = gameObject.GetComponentsInChildren<Rigidbody>();

            foreach (Rigidbody rigidBody in rigidBodies)
            {
                rigidBody.isKinematic = true;
            }
        }
    }
}

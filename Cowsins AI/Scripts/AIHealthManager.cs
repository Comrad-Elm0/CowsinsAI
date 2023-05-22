namespace cowsins.AI
{
public class AIHealthManager : EnemyAI
{
    bool isDead = false;

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

        UIEvents.onEnemyKilled.Invoke(_name);

        base.Die();
    }

    public override void Update()
    {
        base.Update();
    }

    public override void Start()
    {
        base.Start();
    }
}
}

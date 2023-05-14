using UnityEngine;

public class DamagePlayerMelee : MonoBehaviour
{
    public float damageAmount;

    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            collision.gameObject.GetComponent<PlayerStats>().Damage(damageAmount);
        }
    }
}

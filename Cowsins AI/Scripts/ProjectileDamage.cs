using System.Collections;
using UnityEngine;

namespace cowsins.AI
{
    public class ProjectileDamage : MonoBehaviour
    {
        private void Awake()
        {
            StartCoroutine(DestroyAfter());
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                collision.gameObject.GetComponent<PlayerStats>().Damage(5);
                Destroy(gameObject);
            }
            else if (collision.gameObject.layer == 3)
            {
                Destroy(gameObject);
            }
        }

        IEnumerator DestroyAfter()
        {
            yield return new WaitForSeconds(8);
        }
    }
}
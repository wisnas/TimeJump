using UnityEngine;

public class Bounce : MonoBehaviour
{
    [SerializeField] private float bounceForce = 25f;

    [SerializeField] private bool usePadDirection = true;
    private void OnTriggerEnter(Collider other)
    {
       
        if (other.CompareTag("Player"))
        {
            Rigidbody rb = other.GetComponent<Rigidbody>();

            if (rb != null)
            {
            
                rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);

                Vector3 bounceDirection = usePadDirection ? transform.up : Vector3.up;

       
                rb.AddForce(bounceDirection * bounceForce, ForceMode.Impulse);
            }
        }
    }
}


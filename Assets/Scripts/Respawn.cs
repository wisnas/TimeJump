using UnityEngine;

public class Respawn : MonoBehaviour
{
    private GameObject respawnPoint;


    void Start()
    {
        respawnPoint = GameObject.FindWithTag("Respawn");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            other.transform.position = respawnPoint.transform.position;
            other.attachedRigidbody.linearVelocity = Vector3.zero;
        }
    }
}

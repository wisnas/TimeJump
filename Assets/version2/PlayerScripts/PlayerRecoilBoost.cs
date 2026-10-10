using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerRecoilBoost : MonoBehaviour
{

    [SerializeField] private Transform cameraHolder;

    [Header("Input")]
    [SerializeField] private InputActionReference ClickAction;


    private Rigidbody rb;
    private void OnEnable() => ClickAction.action.Enable();
    private void OnDisable() => ClickAction.action.Disable();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        Vector3 cameralook = cameraHolder.transform.forward;

        Vector3 back = -cameralook;

        if (ClickAction.action.WasPressedThisFrame())
        {
            rb.AddForce(back * 10f, ForceMode.Impulse);
            Debug.Log("boom");
        }
    }
}

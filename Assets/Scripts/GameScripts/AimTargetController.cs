using UnityEngine;

public class AimTargetController : MonoBehaviour
{
    [SerializeField] private Transform aimTransform;
    [SerializeField] private float distance = 10f;

    private void Update()
    {
        if (aimTransform == null) return;
        transform.position = aimTransform.position + aimTransform.forward * distance;
    }
}
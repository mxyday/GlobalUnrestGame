using System.Globalization;
using Unity.Netcode;
using UnityEngine;

public class RagdollActivator : NetworkBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private Rigidbody[] ragdollBodies;
    [SerializeField] private Collider[] ragdollColliders;
    [SerializeField] private Collider mainCollider;
    [SerializeField] private Rigidbody mainRigidbody;

    private void Awake()
    {
        SetRagdollState(false);
    }

    public void ActivateRagdoll()
    {
        animator.enabled = false;

        if (mainCollider != null) mainCollider.enabled = false;
        if (mainRigidbody != null) mainRigidbody.isKinematic = true;

        SetRagdollState(true);

        ActivateRagdollServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivateRagdollServerRpc()
    {
        ActivateRagdollClientRpc();
    }

    [ClientRpc]
    private void ActivateRagdollClientRpc()
    {
        if (!IsOwner) // Власник уже активував локально
        {
            SetRagdollState(true);
        }
    }

    private void SetRagdollState(bool state)
    {
        foreach (var rb in ragdollBodies)
        {
            rb.isKinematic = !state;
        }

        foreach (var col in ragdollColliders)
        {
            col.enabled = state;
        }
    }
}

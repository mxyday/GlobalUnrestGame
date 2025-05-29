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
        SetRagdollState(true);
        ActivateRagdollServerRpc();
    }

    public void DeactivateRagdoll()
    {
        SetRagdollState(false);
        DeactivateRagdollServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void ActivateRagdollServerRpc()
    {
        ActivateRagdollClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void DeactivateRagdollServerRpc()
    {
        DeactivateRagdollClientRpc();
    }

    [ClientRpc]
    private void ActivateRagdollClientRpc()
    {
        if (!IsOwner)
        {
            SetRagdollState(true);
        }
    }

    [ClientRpc]
    private void DeactivateRagdollClientRpc()
    {
        if (!IsOwner)
        {
            SetRagdollState(false);
        }
    }

    private void SetRagdollState(bool state)
    {
        // Ragdoll частини
        foreach (var rb in ragdollBodies)
        {
            rb.isKinematic = !state;
        }

        foreach (var col in ragdollColliders)
        {
            col.enabled = state;
        }

        // Головні компоненти
        if (animator != null)
            animator.enabled = !state;

        if (mainCollider != null)
            mainCollider.enabled = !state;

        if (mainRigidbody != null)
            mainRigidbody.isKinematic = state;
    }
}

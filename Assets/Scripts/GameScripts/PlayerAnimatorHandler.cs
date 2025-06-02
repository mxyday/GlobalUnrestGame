using Unity.Netcode;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerAnimatorHandler : NetworkBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private ChainIKConstraint leftHandConstraint;

    private NetworkVariable<float> leftHandWeight = new NetworkVariable<float>(
        1f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
    );

    private void Awake()
    {
        leftHandWeight.OnValueChanged += OnLeftHandWeightChanged;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Синхронізуємо початковий стан для клієнтів
            leftHandConstraint.weight = leftHandWeight.Value;
        }
    }

    private void OnLeftHandWeightChanged(float oldValue, float newValue)
    {
        if (!IsOwner)
        {
            leftHandConstraint.weight = newValue;
        }
    }

    public void SetReloadAnimationSpeed(float reloadDuration)
    {
        if (!IsOwner) return;

        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float animationLength = stateInfo.length > 0 ? stateInfo.length : 1f;
        float speed = animationLength / reloadDuration;
        animator.SetFloat("ReloadSpeed", speed);
    }

    public void SetReloading(bool isReloading)
    {
        animator.SetBool("IsReloading", isReloading);

        if (leftHandConstraint != null)
        {
            float targetWeight = isReloading ? 0f : 1f;
            leftHandConstraint.weight = targetWeight;

            if (IsOwner)
            {
                leftHandWeight.Value = targetWeight;
            }
        }
    }
}
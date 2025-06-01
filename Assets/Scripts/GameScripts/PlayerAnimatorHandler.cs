using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerAnimatorHandler : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private ChainIKConstraint leftHandConstraint;

    public void SetReloadAnimationSpeed(float reloadDuration)
    {
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
        float animationLength = stateInfo.length > 0 ? stateInfo.length : 1f; // Запобігання діленню на 0
        float speed = animationLength / reloadDuration;
        animator.SetFloat("ReloadSpeed", speed);
    }

    public void SetReloading(bool isReloading)
    {
        animator.SetBool("IsReloading", isReloading);

        if (leftHandConstraint != null)
            leftHandConstraint.weight = isReloading ? 0f : 1f;
    }
}

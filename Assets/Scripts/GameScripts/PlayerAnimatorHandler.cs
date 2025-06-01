using UnityEngine;

public class PlayerAnimatorHandler : MonoBehaviour
{
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void PlayReload()
    {
        animator?.SetTrigger("Reload");
    }
}
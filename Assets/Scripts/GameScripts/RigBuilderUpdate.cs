using UnityEngine;
using UnityEngine.Animations.Rigging;

public class ForceRigBuilderUpdate : MonoBehaviour
{
    private RigBuilder rigBuilder;

    private void Awake()
    {
        rigBuilder = GetComponent<RigBuilder>();
    }

    private void LateUpdate()
    {
        if (rigBuilder != null)
        {
            rigBuilder.Build(); // Перебудовує Rig кожен кадр
        }
    }
}
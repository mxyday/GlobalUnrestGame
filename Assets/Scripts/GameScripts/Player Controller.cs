using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerMovement : NetworkBehaviour, IDamageable
{
    [SerializeField] GameObject aimTransform;
    [SerializeField] GameObject cameraObject;

    [SerializeField] float mouseSensitivity, sprintSpeed, walkSpeed, jumpForce, smoothTime;

    [SerializeField] Item[] items;

    float health = 100;
    int itemIndex;
    int previousItemIndex = -1;

    float verticalLookRotation;
    Vector3 smoothMoveVelocity;
    Vector3 moveAmount;
    Rigidbody rb;

    private float moveSpeed = 5f;
    private Animator animator;

    private void Start()
    {
        if (!IsOwner)
        {
            aimTransform.SetActive(false);
            cameraObject.SetActive(false);
        }
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        EquipItem(-1);
    }

    private void Update()
    {
        if (!IsOwner) return;

        Look();

        HandleMovementInput();

        for (int i = 0; i < items.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                EquipItem(i);
                break;
            }
        }

        if (Input.GetMouseButtonDown(0))
        {
            items[itemIndex].Use();
        }
    }

    private void HandleMovementInput()
    {
        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;

        HandleMovementServerRpc(moveDir.normalized);
    }

    private void Look()
    {
        if (Cursor.lockState == CursorLockMode.None)
        {
            return;
        }
        transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity);

        verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -90f, 90f);

        aimTransform.transform.localEulerAngles = Vector3.left * verticalLookRotation;

        UpdateRotationServerRpc(transform.rotation);
    }

    [ServerRpc]
    private void UpdateRotationServerRpc(Quaternion newRotation)
    {
        UpdateRotationClientRpc(newRotation);
    }

    [ClientRpc]
    private void UpdateRotationClientRpc(Quaternion newRotation)
    {
        if (!IsOwner)
        {
            transform.rotation = newRotation;
        }
    }

    void EquipItem(int _index)
    {
        if (_index == previousItemIndex) return;

        itemIndex = _index;

        EquipItemServerRpc(_index);
    }

    [ServerRpc(RequireOwnership = false)]
    private void EquipItemServerRpc(int _index)
    {
        EquipItemClientRpc(_index);
    }

    [ClientRpc]
    private void EquipItemClientRpc(int _index)
    {
        if (_index == previousItemIndex) return;

        itemIndex = _index;

        items[itemIndex].ItemGameObject.SetActive(true);

        if (previousItemIndex != -1)
        {
            items[previousItemIndex].ItemGameObject.SetActive(false);
        }

        previousItemIndex = itemIndex;
    }

    [ServerRpc(RequireOwnership = false)]
    private void HandleMovementServerRpc(Vector3 moveDir)
    {
        moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed), ref smoothMoveVelocity, smoothTime);

        UpdateAnimationClientRpc(moveDir.magnitude);
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 newPosition)
    {
        if (!IsOwner)
        {
            transform.position = newPosition;
        }
    }

    [ClientRpc]
    private void UpdateAnimationClientRpc(float speed)
    {
        if (animator != null)
        {
            animator.SetFloat("Speed", speed);
        }
    }

    private void FixedUpdate()
    {
        if (IsServer)
        {
            Vector3 newPosition = rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime;

            rb.MovePosition(newPosition);

            UpdatePositionClientRpc(newPosition);
        }
    }

    public void TakeDamage(float damage)
    {
        Debug.Log("Took damage: " + damage); // Ïðàöþº
        health -= damage;
        if (health <= 0)
        {
            Death();
        }
    }

    private void Death()
    {
        Destroy(gameObject);
    }
}
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class PlayerController : NetworkBehaviour, IDamageable
{
    [SerializeField] RigBuilder rigBuilder;
    [SerializeField] GameObject aimTransform;
    [SerializeField] GameObject cameraObject;
    [SerializeField] private Transform adsPosition;
    [SerializeField] private Transform owPosition;
    [SerializeField] private Transform weaponRoot;
    [SerializeField] private float aimSpeed = 10f;

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

    private bool isAlive = true;
    private bool isAiming;

    private TickRunner networkTickRunner = new TickRunner();

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

        if (!isAlive) return;

        Look();

        HandleMovementInput();

        Aim();

        for (int i = 0; i < items.Length; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                EquipItem(i);
                break;
            }
        }

        if (Input.GetMouseButton(0))
        {
            items[itemIndex].Use();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            Die();
        }

        if (Input.GetKeyDown(KeyCode.T))
        {
            Test();
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

        // Обертання гравця вліво/вправо по горизонталі (Yaw)
        transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity);

        // Обертання камери вгору/вниз по вертикалі (Pitch)
        verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        // Клэмпимо вертикальний кут у межах -75 до +75
        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -75f, 75f);

        // Обновляємо поворот aimTransform тільки по X осі (інверсія напряму)
        aimTransform.transform.localEulerAngles = new Vector3(-verticalLookRotation, 0f, 0f);

        // Відправляємо оновлення на сервер
        UpdateRotationServerRpc(transform.rotation);
    }

    [ServerRpc]
    private void UpdateRotationServerRpc(Quaternion newRotation)
    {
        networkTickRunner.Tick(() => UpdateRotationClientRpc(newRotation), Time.deltaTime);
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
        networkTickRunner.Tick(() => EquipItemClientRpc(_index), Time.deltaTime);
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
        networkTickRunner.Tick(() =>
        {
            moveAmount = Vector3.SmoothDamp(moveAmount, moveDir * (Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed), ref smoothMoveVelocity, smoothTime);

            UpdateAnimationClientRpc(moveDir.magnitude);
        }, Time.deltaTime);
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
        networkTickRunner.Tick(() =>
        {
                if (IsServer)
                {
                    Vector3 newPosition = rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime;

                    rb.MovePosition(newPosition);

                    UpdatePositionClientRpc(newPosition);
                }
        }, Time.deltaTime);
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 newPosition)
    {
        if (!IsOwner)
        {
            transform.position = newPosition;
        }
    }

    private void Aim()
    {
        if (Input.GetMouseButton(1))
            isAiming = true;
        else
            isAiming = false;

        if (isAiming)
        {
            weaponRoot.position = Vector3.Lerp(weaponRoot.position, adsPosition.position, Time.deltaTime * aimSpeed);
        }
        else
        {
            weaponRoot.position = Vector3.Lerp(weaponRoot.position, owPosition.position, Time.deltaTime * aimSpeed);
        }
    }

    public void TakeDamage(float damage)
    {
        Debug.Log("Took damage: " + damage); // Працює
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        ActivateRagdollClientRpc();
    }

    [ClientRpc]
    private void ActivateRagdollClientRpc()
    {
        isAlive = false;
        GetComponent<RagdollActivator>()?.ActivateRagdoll();
    }

    public void Resurrect()
    {
        isAlive = true;
    }

    private void Test()
    {
        rigBuilder.enabled = false;
        rigBuilder.enabled = true;
    }
}
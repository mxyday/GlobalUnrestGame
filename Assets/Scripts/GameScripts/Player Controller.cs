using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Linq;

public class PlayerController : NetworkBehaviour, IDamageable
{
    [SerializeField] RigBuilder rigBuilder;
    [SerializeField] GameObject aimTransform;
    [SerializeField] GameObject cameraObject;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform adsPosition;
    [SerializeField] private Transform owPosition;
    [SerializeField] private Transform weaponRoot;
    [SerializeField] private float aimSpeed = 10f;

    [SerializeField] float mouseSensitivity, sprintSpeed, walkSpeed, jumpForce, smoothTime;

    [SerializeField] private WeaponLoadout[] availableLoadouts;
    [SerializeField] private GameObject[] allItems;
    private Item[] items;

    [SerializeField] private float defaultFOV = 60f;
    [SerializeField] private float aimFOV = 40f;
    [SerializeField] private float fovSpeed = 10f;

    [SerializeField] private float rotationSmoothSpeed = 10f;
    private Quaternion targetRotation;

    private float targetFOV;

    private GameObject currentWeapon;

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

        SetClass(0);
        EquipItem(0);
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

        if (!IsServer && !IsOwner)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
        }

        for (int i = 0; i < items.Length; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1 + i))
            {
                EquipItem(i);
                break;
            }
        }

        if (Input.GetMouseButton(0) && items != null)
        {
            items[itemIndex].Use();
        }

        if (Input.GetKeyDown(KeyCode.R) && items != null)
        {
            if (items[itemIndex] is Gun gun)
            {
                gun.Reload();
            }
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
            targetRotation = newRotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);
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
                // ОНОВЛЮЄМО ОБЕРТАННЯ ПЕРЕД РУХОМ
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothSpeed);

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
        {
            isAiming = true;
            Debug.Log("Aiming");
        }
        else
            isAiming = false;

        Vector3 targetPos = isAiming ? adsPosition.position : owPosition.position;
        weaponRoot.position = Vector3.Lerp(weaponRoot.position, targetPos, Time.deltaTime * aimSpeed);

        float targetFOV = isAiming ? aimFOV : defaultFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovSpeed);
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

    public void SetClass(int classIndex)
    {
        if (classIndex < 0 || classIndex >= availableLoadouts.Length) return;

        // Вимикаємо всю зброю
        foreach (var item in allItems)
        {
            item.SetActive(false);
        }

        var loadout = availableLoadouts[classIndex];

        // Активуємо лише ті, що входять у набір
        foreach (var classitem in loadout.classItems)
        {
            var item = allItems.FirstOrDefault(w => w.name == classitem);
            if (item != null)
            {
                item.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"Weapon '{classitem}' not found in allItems list!");
            }
        }

        // Оновлюємо список активних предметів для перемикання
        items = allItems
            .Where(w => w.activeSelf && w.TryGetComponent<Item>(out _))
            .Select(w => w.GetComponent<Item>())
            .ToArray();

        EquipItem(0); // Автоматично екіпувати першу зброю
    }
}
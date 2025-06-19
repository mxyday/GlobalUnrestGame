using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class PlayerController : NetworkBehaviour, IDamageable
{
    [SerializeField] RigBuilder rigBuilder;
    [SerializeField] GameObject aimTransform;
    [SerializeField] GameObject cameraObject;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private List<Transform> adsPositions;
    [SerializeField] private Transform owPosition;
    [SerializeField] private Transform weaponRoot;
    [SerializeField] private float aimSpeed = 10f;

    [SerializeField] float mouseSensitivity, sprintSpeed, walkSpeed, jumpForce, smoothTime;
    [SerializeField] private float interpolationSpeed = 15f;

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

    public bool isAlive = true;
    private bool isAiming;
    private bool isGrounded = true;
    public bool isRespawning = false;

    private bool canJump = true;

    private TickRunner networkTickRunner = new TickRunner();

    private Vector3 targetPosition;

    private float respawnTimer = 5f;
    private float respawnCountdown;

    private void Start()
    {
        if (!IsOwner)
        {
            aimTransform.SetActive(false);
            cameraObject.SetActive(false);
        }

        SetClass(0);
        if (IsOwner)
        {
            Invoke(nameof(InitializeEquipment), 0.1f);
        }
    }

    private void InitializeEquipment()
    {
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

        if (!isAlive)
        {
            HandleRespawnCountdown();
            return; // Блокуємо всі дії, якщо гравець мертвий
        }

        Look();

        HandleMovementInput();

        Aim();

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

        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            Jump();
        }

        if (Input.GetKeyDown(KeyCode.K))
        {
            Die();
        }

        // Блокуємо доступ до меню (Escape) під час респавну
        if (Input.GetKeyDown(KeyCode.Escape) && !isRespawning)
        {
            bool isActive = GameObject.Find("GameMenu")?.GetComponent<GameMenu>()?.gameObject.activeSelf ?? false;
            GameObject.Find("GameMenu")?.GetComponent<GameMenu>()?.gameObject.SetActive(!isActive);

            Cursor.lockState = isActive ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isActive;
        }
    }

    private void HandleRespawnCountdown()
    {
        if (isRespawning)
        {
            respawnCountdown -= Time.deltaTime;

            if (respawnCountdown <= 0)
            {
                isRespawning = false;
                GetComponent<PlayerSettings>()?.RequestRespawn();
            }
        }
    }

    private void HandleMovementInput()
    {
        if (!isAlive || isRespawning) return;

        Vector3 moveDir = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical")).normalized;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift);
        HandleMovementServerRpc(moveDir, isSprinting);
    }

    private void Look()
    {
        if (!isAlive || isRespawning || Cursor.lockState == CursorLockMode.None) return;

        transform.Rotate(Vector3.up * Input.GetAxisRaw("Mouse X") * mouseSensitivity);

        verticalLookRotation += Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        verticalLookRotation = Mathf.Clamp(verticalLookRotation, -75f, 75f);

        aimTransform.transform.localEulerAngles = new Vector3(-verticalLookRotation, 0f, 0f);

        UpdateRotationServerRpc(transform.rotation);
    }

    [ServerRpc]
    private void UpdateRotationServerRpc(Quaternion newRotation)
    {
        networkTickRunner.Tick(() => UpdateRotationClientRpc(newRotation), Time.fixedDeltaTime);
    }

    [ClientRpc]
    private void UpdateRotationClientRpc(Quaternion newRotation)
    {
        if (!IsOwner)
        {
            targetRotation = newRotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSmoothSpeed);
        }
    }

    void EquipItem(int _index)
    {
        if (_index == previousItemIndex || !isAlive || isRespawning) return;

        itemIndex = _index;

        EquipItemServerRpc(_index);
    }

    [ServerRpc(RequireOwnership = false)]
    private void EquipItemServerRpc(int _index)
    {
        networkTickRunner.Tick(() => EquipItemClientRpc(_index), Time.fixedDeltaTime);
    }

    [ClientRpc]
    private void EquipItemClientRpc(int _index)
    {
        if (_index == previousItemIndex || items == null || _index < 0 || _index >= items.Length || !isAlive || isRespawning)
        {
            Debug.LogWarning($"EquipItemClientRpc failed: index={_index}, items={(items == null ? "null" : items.Length.ToString())}");
            return;
        }

        itemIndex = _index;

        items[itemIndex].ItemGameObject.SetActive(true);

        if (previousItemIndex != -1)
        {
            items[previousItemIndex].ItemGameObject.SetActive(false);
        }

        previousItemIndex = itemIndex;
    }

    private void Jump()
    {
        if (!IsOwner || !isAlive || isRespawning) return;

        JumpServerRpc();
    }

    [ServerRpc]
    private void JumpServerRpc()
    {
        networkTickRunner.Tick(() =>
        {
            if (canJump)
            {
                rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                isGrounded = false;
                canJump = false;
            }
        }, Time.fixedDeltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!isAlive || isRespawning) return;
        isGrounded = true;
        Invoke(nameof(ResetJump), 3f);
    }

    private void ResetJump()
    {
        if (!isAlive || isRespawning) return;
        canJump = true;
    }

    [ServerRpc]
    private void HandleMovementServerRpc(Vector3 moveDir, bool isSprinting)
    {
        if (!isAlive || isRespawning) return;
        moveAmount = moveDir * (isSprinting ? sprintSpeed : walkSpeed);
        UpdateAnimationClientRpc(moveDir.magnitude);
    }

    [ClientRpc]
    private void UpdateAnimationClientRpc(float speed)
    {
        if (animator != null && !isRespawning)
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
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSmoothSpeed);

                Vector3 newPosition = rb.position + transform.TransformDirection(moveAmount) * Time.fixedDeltaTime;
                rb.MovePosition(newPosition);

                UpdatePositionClientRpc(newPosition);
            }
            else if (!IsOwner)
            {
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.fixedDeltaTime * interpolationSpeed);
            }
        }, Time.fixedDeltaTime);
    }

    [ClientRpc]
    private void UpdatePositionClientRpc(Vector3 newPosition)
    {
        if (!IsOwner)
        {
            targetPosition = newPosition;
        }
    }

    private void Aim()
    {
        if (!isAlive || isRespawning) return;

        if (Input.GetMouseButton(1))
        {
            isAiming = true;
            Debug.Log("Aiming");
        }
        else
        {
            isAiming = false;
        }

        SingleShotGun weaponData = items[itemIndex].GetComponent<SingleShotGun>();
        Transform currentAdsPosition = (weaponData != null) ? weaponData.ADSPosition : owPosition;
        Vector3 targetPos = isAiming ? currentAdsPosition.position : owPosition.position;
        weaponRoot.position = Vector3.Lerp(weaponRoot.position, targetPos, Time.deltaTime * aimSpeed);

        float targetFOV = isAiming ? aimFOV : defaultFOV;
        playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFOV, Time.deltaTime * fovSpeed);
    }

    public void TakeDamage(float damage)
    {
        Debug.Log($"Took damage: {damage}");
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }

    public void Die()
    {
        if (isRespawning) return;
        isRespawning = true;
        respawnCountdown = respawnTimer;
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
        isRespawning = false;
    }

    public void SetClass(int classIndex)
    {
        if (classIndex < 0 || classIndex >= availableLoadouts.Length) return;

        foreach (var item in allItems)
        {
            item.SetActive(false);
        }

        var loadout = availableLoadouts[classIndex];

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

        items = allItems
            .Where(w => w.activeSelf && w.TryGetComponent<Item>(out _))
            .Select(w => w.GetComponent<Item>())
            .ToArray();

        if (adsPositions != null && adsPositions.Count != allItems.Length)
        {
            Debug.LogWarning($"Number of adsPositions ({adsPositions.Count}) does not match number of allItems ({allItems.Length}). Ensure they are synchronized!");
        }

        Debug.Log($"SetClass: items initialized with length={items.Length}");
    }
}
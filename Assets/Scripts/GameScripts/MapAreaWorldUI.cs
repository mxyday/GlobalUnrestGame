using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Globalization;
using Unity.Netcode;

public class MapAreaWorldUI : NetworkBehaviour
{
    [SerializeField] private Image circleFillImage;
    [SerializeField] private TextMeshProUGUI letterText;

    private Transform cameraTransform;
    private bool cameraAssigned = false;
    private Vector3 initialScale;

    private void Awake()
    {
        initialScale = transform.localScale;
    }

    private void LateUpdate()
    {
        if (!cameraAssigned)
        {
            TryFindLocalCamera();
        }

        if (cameraTransform != null)
        {
            FaceCamera();
        }
    }

    public void SetLetter(string letter)
    {
        letterText.text = letter;
    }

    public void SetProgress(float progress)
    {
        circleFillImage.fillAmount = Mathf.Abs(progress);

        if (progress > 0)
            circleFillImage.color = HexColor("#00FFF0");
        else if (progress < 0)
            circleFillImage.color = HexColor("#FF9800");
        else
            circleFillImage.color = Color.gray;
    }

    private Color HexColor(string hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(hex, out color))
            return color;

        Debug.LogWarning($"Invalid HEX color: {hex}");
        return Color.white;
    }

    private void TryFindLocalCamera()
    {
        var players = FindObjectsByType<PlayerSettings>(FindObjectsSortMode.None);

        foreach (PlayerSettings player in players)
        {
            if (player.IsOwner)
            {
                Camera foundCamera = player.GetComponentInChildren<Camera>();
                if (foundCamera != null)
                {
                    cameraTransform = foundCamera.transform;
                    cameraAssigned = true;
                    break;
                }
            }
        }
    }

    private void FaceCamera()
    {
        Vector3 direction = (cameraTransform.position - transform.position).normalized;
        direction.y = 0f;

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = lookRotation;
        }

        transform.localScale = initialScale;
    }
}

using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Globalization;
using Unity.Netcode;

public class MapAreaWorldUI : NetworkBehaviour
{

    [SerializeField] private Image circleFillImage;
    [SerializeField] private TextMeshProUGUI letterText;
    [SerializeField] private Transform lookTarget;

    private Camera playerCamera;

    private void Start()
    {
        if (!IsOwner) return;

        // ¬изначити камеру гравц€ Ч може бути через тег або окремий клас
        playerCamera = Camera.main;
    }

    private void Update()
    {
        if (!IsOwner || playerCamera == null) return;

        FaceCamera(playerCamera);
    }

    public void SetLetter(string letter)
    {
        letterText.text = letter;
    }

    public void SetProgress(float progress)
    {
        circleFillImage.fillAmount = Mathf.Abs(progress);

        if (progress > 0)
            circleFillImage.color = HexColor("#00FFF0"); // Team A Ч б≥рюзовий
        else if (progress < 0)
            circleFillImage.color = HexColor("#FF9800"); // Team B Ч помаранчевий
        else
            circleFillImage.color = Color.gray; // нейтральний
    }

    private Color HexColor(string hex)
    {
        Color color;
        if (ColorUtility.TryParseHtmlString(hex, out color))
            return color;

        Debug.LogWarning($"Invalid HEX color: {hex}");
        return Color.white;
    }

    private void FaceCamera(Camera cam)
    {
        Vector3 direction = cam.transform.position - lookTarget.position;
        direction.y = 0f; // €кщо не хочеш, щоб крутивс€ по вертикал≥
        lookTarget.forward = direction.normalized;
    }
}

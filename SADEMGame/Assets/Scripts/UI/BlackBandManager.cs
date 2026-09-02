using UnityEngine;

public class BlackBandsManager : MonoBehaviour
{
    [Header("Bant Referanslari")]
    public RectTransform topBand;
    public RectTransform bottomBand;

    private Rect lastSafeArea = new Rect(0, 0, 0, 0);

    void Start()
    {
        ApplyBands();
    }

    void Update()
    {
        if (lastSafeArea != Screen.safeArea)
        {
            ApplyBands();
        }
    }

    void ApplyBands()
    {
        Rect safeArea = Screen.safeArea;
        lastSafeArea = safeArea;

        float safeAreaTopY = (safeArea.y + safeArea.height) / Screen.height;
        float safeAreaBottomY = safeArea.y / Screen.height;


        if (topBand != null)
        {
            topBand.anchorMin = new Vector2(0, safeAreaTopY);
            topBand.anchorMax = new Vector2(1, 1);
            topBand.offsetMin = Vector2.zero;
            topBand.offsetMax = Vector2.zero;
        }

        // --- ALT BANT ---
        if (bottomBand != null)
        {
            bottomBand.anchorMin = new Vector2(0, 0);
            bottomBand.anchorMax = new Vector2(1, safeAreaBottomY);
            bottomBand.offsetMin = Vector2.zero;
            bottomBand.offsetMax = Vector2.zero;
        }
    }
}
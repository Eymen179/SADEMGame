using UnityEngine;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

public class FallingWord : MonoBehaviour
{
    [Header("UI Referanslari")]
    public TextMeshProUGUI txtEnglishWord;

    // Kelime listesi
    [HideInInspector] public List<string> targetTurkishWords;
    [HideInInspector] public FastTypingManager gameManager;

    private RectTransform rectTransform;

    public void Initialize(string englishText, List<string> turkishTexts, float speed, float limitY, FastTypingManager manager)
    {
        txtEnglishWord.text = englishText;
        targetTurkishWords = turkishTexts;
        gameManager = manager;

        rectTransform = GetComponent<RectTransform>();

        float distance = Mathf.Abs(rectTransform.anchoredPosition.y - limitY);
        float duration = distance / speed;

        rectTransform.DOAnchorPosY(limitY, duration)
            .SetEase(Ease.Linear)
            .OnComplete(OnWordReachedBottom);
    }

    void OnWordReachedBottom()
    {
        gameManager.OnWordMissed(this);
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        rectTransform.DOKill();
    }
}
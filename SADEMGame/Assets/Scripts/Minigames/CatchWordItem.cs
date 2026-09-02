using UnityEngine;
using TMPro;

public class CatchWordItem : MonoBehaviour
{
    public TextMeshProUGUI txtPickableWord;

    [HideInInspector] public string assignedEnglishWord;
    private CatchWordManager manager;

    // Hiz parametresini kaldirdik, artik hizi Rigidbody Gravity belirleyecek
    public void Initialize(string word, CatchWordManager gameManager)
    {
        assignedEnglishWord = word;
        manager = gameManager;

        txtPickableWord.text = FormatWordTitleCase(word, "en-US");
    }

    string FormatWordTitleCase(string word, string cultureInfo)
    {
        if (string.IsNullOrEmpty(word)) return word;
        var culture = new System.Globalization.CultureInfo(cultureInfo);
        return char.ToUpper(word[0], culture) + word.Substring(1).ToLower(culture);
    }

    // YENI: Kati carpisma yerine yeniden Trigger (Tetikleyici) sistemine donduk
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.name == "BasketPart (4)")
        {
            manager.OnWordCaught(this);
        }
    }

    private void Update()
    {
        // Kutu yakalanamadan asagi dusup ekrandan cikarsa bellegi rahatlatmak icin yok et
        if (transform.position.y < -10f)
        {
            Destroy(gameObject);
        }
    }
}
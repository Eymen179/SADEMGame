using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class VersionDisplay : MonoBehaviour
{
    private TextMeshProUGUI txtVersionDisplay;
    void Start()
    {
        txtVersionDisplay = GetComponent<TextMeshProUGUI>();

        if (txtVersionDisplay != null)
        {
            txtVersionDisplay.text = "v" + Application.version;
        }
    }
}
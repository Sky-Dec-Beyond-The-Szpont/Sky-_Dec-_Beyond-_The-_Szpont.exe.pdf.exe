using TMPro;
using UnityEngine;

public class DebugOverlay : MonoBehaviour
{
    public TMP_Text txt;

    void Awake()
    {
        if (txt == null)
            txt = GetComponentInChildren<TMP_Text>();
    }

    public void SetText(string s)
    {
        if (txt != null) txt.text = s;
    }
}

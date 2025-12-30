using UnityEngine;

public class HealthBarUI : MonoBehaviour
{
    public float Health = 100, MaxHealth = 100, width = 100;

    [SerializeField]
    private RectTransform healthBar;

    public void SetMaxHealth(float maxHealth)
    {
        MaxHealth = maxHealth;
    }

    public void SetHealth(float health)
    {
        Health = health;
        float newWidth = (Health / MaxHealth) * width;

        healthBar.sizeDelta = new Vector2 (newWidth, healthBar.sizeDelta.y);
    }

}

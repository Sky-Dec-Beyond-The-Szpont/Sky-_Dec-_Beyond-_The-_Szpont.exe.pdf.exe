using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public int health = 100;


    [SerializeField]
    private HealthBarUI healthBar;
    public void TakeDamage(int dmg)
    {
        health -= dmg;
        Debug.Log("Player health: " + health);

        healthBar.SetHealth(health);

        if (health <= 0)
        {
            if (TowerClick.chosenTower != null)
            {
                TowerClick.chosenTower.ReturnFromTower();
            }
            else
            {
                Debug.LogWarning("chosenTower == null – brak wie¿y do powrotu");
            }
        }
    }
}

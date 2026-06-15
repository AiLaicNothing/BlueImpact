using UnityEngine;
using UnityEngine.UI;

public class E_HealthBar : MonoBehaviour
{
    [SerializeField] private Slider healthBar;
    private void Awake()
    {
        healthBar = GetComponent<Slider>();
    }

    public void UpdateHealthBar(float currentHp, float maxHp)
    {
        healthBar.value = currentHp / maxHp;
    }
}

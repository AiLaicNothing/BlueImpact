using UnityEngine;
using UnityEngine.UI;

public class E_StaggerBar : MonoBehaviour
{
    [SerializeField] private Slider staggerBar;

    private void Awake()
    {
        staggerBar = GetComponent<Slider>();
    }
    public void UpdateStaggerhBar(float currentHp, float maxHp)
    {
        staggerBar.value = currentHp / maxHp;
    }
}

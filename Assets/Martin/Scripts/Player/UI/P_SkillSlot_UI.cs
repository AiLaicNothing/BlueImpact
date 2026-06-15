using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class P_SkillSlot_UI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private Image cooldownFill;
    [SerializeField] private TextMeshProUGUI cooldownText;

    public void SetEmpty()
    {
        icon.enabled = false;

        if (cooldownFill != null)
            cooldownFill.fillAmount = 0f;

        cooldownText.gameObject.SetActive(false);
    }

    public void SetIcon(Sprite sprite)
    {
        icon.enabled = true;
        icon.sprite = sprite;
    }

    public void UpdateCooldown(float remaining, float maxCooldown)
    {
        if (remaining <= 0f)
        {
            cooldownText.gameObject.SetActive(false);

            if (cooldownFill != null) cooldownFill.fillAmount = 0f;

            return;
        }

        cooldownText.gameObject.SetActive(true);
        cooldownText.text = Mathf.CeilToInt(remaining).ToString();

        if (cooldownFill != null && maxCooldown > 0f)
        {
            cooldownFill.fillAmount = remaining / maxCooldown;
        }
    }
}

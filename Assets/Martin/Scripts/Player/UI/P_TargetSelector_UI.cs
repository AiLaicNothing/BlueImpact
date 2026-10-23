using UnityEngine;
using UnityEngine.UI;

public class P_TargetSelector_UI : MonoBehaviour
{
    public static P_TargetSelector_UI Instance;

    public GameObject Image;
    public Image aimIcon;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public GameObject GetIcon()
    {
        return Image;
    }

    public void SetIcon(Sprite sprite)
    {
        if (Image == null) return;
   
        aimIcon.sprite = sprite;
    }

    public void ShowIcon()
    {
        Image.SetActive(true);
    }

    public void HideIcon()
    {
        Image.SetActive(false);
    }
}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 🎮 CharButtonNavigationSetup - Configura Navigation de botones automáticamente
/// 
/// Simplemente coloca este script en un GameObject en tu CharSelector
/// y asigna los 3 botones en el Inspector. 
/// Se ejecutará automáticamente en Awake para configurar la navigation.
/// </summary>
public class CharButtonNavigationSetup : MonoBehaviour
{
    [SerializeField] private Button[] charButtons = new Button[3];

    private void Awake()
    {
        SetupNavigation();
    }

    private void SetupNavigation()
    {
        if (charButtons.Length != 3)
        {
            Debug.LogError("❌ Necesitas exactamente 3 botones en el array");
            return;
        }

        // Botón 0 (arriba → 2, abajo → 1)
        var nav0 = charButtons[0].navigation;
        nav0.mode = Navigation.Mode.Explicit;
        nav0.selectOnUp = charButtons[2];
        nav0.selectOnDown = charButtons[1];
        nav0.selectOnLeft = null;
        nav0.selectOnRight = null;
        charButtons[0].navigation = nav0;

        // Botón 1 (arriba → 0, abajo → 2)
        var nav1 = charButtons[1].navigation;
        nav1.mode = Navigation.Mode.Explicit;
        nav1.selectOnUp = charButtons[0];
        nav1.selectOnDown = charButtons[2];
        nav1.selectOnLeft = null;
        nav1.selectOnRight = null;
        charButtons[1].navigation = nav1;

        // Botón 2 (arriba → 1, abajo → 0)
        var nav2 = charButtons[2].navigation;
        nav2.mode = Navigation.Mode.Explicit;
        nav2.selectOnUp = charButtons[1];
        nav2.selectOnDown = charButtons[0];
        nav2.selectOnLeft = null;
        nav2.selectOnRight = null;
        charButtons[2].navigation = nav2;

        Debug.Log("✅ Navigation configurada correctamente para los 3 botones");
    }
}
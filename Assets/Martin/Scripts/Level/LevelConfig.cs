using UnityEngine;

// Configuración de un nivel. Un asset por zona (Tutorial, Zone_00..Zone_05).
[CreateAssetMenu(menuName = "Level/Level Config", fileName = "LevelConfig_")]
public class LevelConfig : ScriptableObject
{
    [Tooltip("ID único del nivel. Usar el nombre de la escena, ej. Zone_02.")]
    [SerializeField] private string levelId = "Zone_00";

    [SerializeField] private string displayName = "Nivel";

    [Tooltip("Proporción mínima de enemigos contables que hay que derrotar (0 a 1).")]
    [Range(0f, 1f)]
    [SerializeField] private float killThreshold = 0.7f;

    public string LevelId => levelId;
    public string DisplayName => displayName;
    public float KillThreshold => killThreshold;

    // Enemigos requeridos para un total dado. Ej: 20 * 0.7 = 14; 21 * 0.7 = 15.
    public int RequiredKills(int total)
    {
        if (total <= 0) return 0;

        // El margen evita que un error de float (ej. 14.0000001) suba el requerido a 15.
        return Mathf.CeilToInt(total * killThreshold - 0.0001f);
    }
}

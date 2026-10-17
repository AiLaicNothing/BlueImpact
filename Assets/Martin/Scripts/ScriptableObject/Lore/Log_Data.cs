using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "SO/Logs")]
public class Log_Data : ScriptableObject
{
    public string logName;
    public string logDescription;
    public Sprite logIllustration;
}

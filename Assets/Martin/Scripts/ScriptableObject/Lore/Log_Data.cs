using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "SO/Logs")]
public class Log_Data : ScriptableObject
{
    public int log_ID;

    public string logName;
    [TextArea(15,20)]
    public string logDescription;
    public Sprite logIllustration;
}

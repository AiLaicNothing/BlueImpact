using UnityEngine;
using UnityEngine.Video;

[System.Serializable]
public class PopUpPage 
{
    public string title;

    [TextArea(5, 20)]
    public string description;

    public Texture texture;
    public VideoClip video;
}

using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public enum Speaker
{
    NPC,
    Player,
    Narrator
}

[System.Serializable]
public class DialogueStep
{
    public Speaker speaker;

    [TextArea(3, 5)]
    public string text;

    public AudioClip audio;

    public CinemachineVirtualCamera virtualCamera;

    [Header("Imagen opcional")]
    public Sprite leftImage;
    public Sprite rightImage;
    public Sprite centerImage;
}

[System.Serializable]
public class DialogueSequence
{
    public List<DialogueStep> steps = new List<DialogueStep>();
}
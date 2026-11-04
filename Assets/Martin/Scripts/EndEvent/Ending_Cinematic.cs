using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

// Los tres finales del juego
public enum EndingType
{
    Ambicion = 0,    // Final 1: absorbe el Blue Impact
    Destruccion = 1, // Final 2: lo destruye y sucumbe a la corrupcion
    Curacion = 2     // Final 3: lo destruye con el objeto secreto y sobrevive
}

public class Ending_Cinematic : MonoBehaviour
{
    // Cada final tiene su video por personaje. Si solo pones uno, se usa para los dos.
    [Serializable]
    public class EndingClips
    {
        public VideoClip lune;
        public VideoClip solis;

        public bool HasAny => lune != null || solis != null;

        public VideoClip For(string characterName)
        {
            if (characterName == "Lune" && lune != null) return lune;
            if (characterName == "Solis" && solis != null) return solis;

            // Si falta el del personaje se usa el que haya, para no quedarse sin video
            return lune != null ? lune : solis;
        }
    }

    public static Ending_Cinematic Instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject video;
    [SerializeField] private GameObject continueScreen;
    [SerializeField] private RawImage videoScreen;
    [SerializeField] private VideoPlayer videoPlayer;

    private CharacterInfo info;

    public Texture texture;

    [Header("Video generico (el que ya estaba)")]
    public VideoClip lune;
    public VideoClip solis;

    [Header("Final 1: Ambicion (absorbe el Blue Impact)")]
    [SerializeField] private EndingClips ambicion;

    [Header("Final 2: Destruccion (lo rompe y sucumbe)")]
    [SerializeField] private EndingClips destruccion;

    [Header("Final 3: Curacion (lo rompe con el objeto secreto)")]
    [SerializeField] private EndingClips curacion;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (panel != null) panel.SetActive(false);
    }

    public void activePanel(bool state)
    {
        if (panel != null) panel.SetActive(state);
    }

    public void activeVideo(bool state)
    {
        if (video != null) video.SetActive(state);
    }

    public void ContinueVideo(bool state)
    {
        if (continueScreen != null) continueScreen.SetActive(state);
    }

    // Cinematica original: elige el video solo por personaje
    public void ShowCutscene()
    {
        PlayClip(GetCharacterName() == "Lune" ? lune : solis);
    }

    // Cinematica de un final concreto. La llama BlueImpactEvent.
    public void ShowEnding(EndingType ending)
    {
        EndingClips set = GetClips(ending);

        if (set == null || !set.HasAny)
        {
            Debug.LogWarning("[Ending] El final " + ending + " no tiene video asignado, se usa el generico");
            ShowCutscene();
            return;
        }

        Debug.Log("[Ending] Reproduciendo final: " + ending);

        PlayClip(set.For(GetCharacterName()));
    }

    // Duracion real del video en curso, para no esperar un numero fijo en el codigo
    public float CurrentClipLength
    {
        get
        {
            if (videoPlayer == null || videoPlayer.clip == null) return 0f;

            return (float)videoPlayer.clip.length;
        }
    }

    private EndingClips GetClips(EndingType ending)
    {
        switch (ending)
        {
            case EndingType.Ambicion: return ambicion;
            case EndingType.Destruccion: return destruccion;
            case EndingType.Curacion: return curacion;
        }

        return null;
    }

    private string GetCharacterName()
    {
        if (PlayerSpawn_Manager.Instance == null) return "";

        info = PlayerSpawn_Manager.Instance.GetCharacter();

        return info.data != null ? info.data.characterName : "";
    }

    private void PlayClip(VideoClip clip)
    {
        activePanel(true);

        if (panel == null) return;

        if (videoPlayer != null)
        {
            videoPlayer.Stop();

            if (clip != null) videoPlayer.clip = clip;
        }

        if (videoScreen != null) videoScreen.texture = texture;

        activeVideo(true);

        if (videoPlayer != null) videoPlayer.Play();
    }
}

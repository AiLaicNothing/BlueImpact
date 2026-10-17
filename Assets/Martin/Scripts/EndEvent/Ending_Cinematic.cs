using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Ending_Cinematic : MonoBehaviour
{
    public static Ending_Cinematic Instance;

    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject video;
    [SerializeField] private GameObject continueScreen;
    [SerializeField] private RawImage videoScreen;
    [SerializeField] private VideoPlayer videoPlayer;

    private CharacterInfo info;

    public Texture texture;
    public VideoClip lune;
    public VideoClip solis;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;  

        panel.SetActive(false);
    }

    public void activePanel(bool state)
    {
        panel.SetActive(state);
    }

    public void activeVideo(bool state)
    {
        video.SetActive(state);
    }

    public void ContinueVideo(bool state)
    {
        continueScreen.SetActive(state);
    }

    public void ShowCutscene()
    {
        info = PlayerSpawn_Manager.Instance.GetCharacter();

        activePanel(true);

        if (panel == null) return;

        if (videoPlayer != null)
        {
            videoPlayer.Stop();

            if (info.data.characterName == "Lune") videoPlayer.clip = lune;

            else if (info.data.characterName == "Solis") videoPlayer.clip = solis;
        }

        if (videoScreen != null)
        {
            videoScreen.texture = texture;
        }

        activeVideo(true);

        videoPlayer.Play();
    }
}

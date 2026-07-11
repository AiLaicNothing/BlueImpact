using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class Ending_Cinematic : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject video;
    [SerializeField] private GameObject continueScreen;
    [SerializeField] private RawImage videoScreen;
    [SerializeField] private VideoPlayer videoPlayer;

    private CharacterInfo info;

    public Texture texture;
    public VideoClip lune;
    public VideoClip solis;

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
        activePanel(true);

        info = PlayerSpawn_Manager.Instance.GetCharacter();

        if (panel == null) return;

        if (videoPlayer != null)
        {
            videoPlayer.Stop();

            if (info.characterName == "Lune") videoPlayer.clip = lune;

            else if (info.characterName == "Solis") videoPlayer.clip = solis;
        }

        if (videoScreen != null)
        {
            videoScreen.texture = texture;
        }
    }
}

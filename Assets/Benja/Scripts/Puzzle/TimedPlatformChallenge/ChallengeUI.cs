using TMPro;
using UnityEngine;

public class ChallengeUI : MonoBehaviour
{
    public static ChallengeUI Instance;

    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text timerText;

    private TimedPlatformChallenge currentChallenge;

    private void Awake()
    {
        Instance = this;

        root.SetActive(false);
    }

    private void Update()
    {
        if (currentChallenge == null)
            return;

        float time =
            Mathf.Max(0f, currentChallenge.RemainingTime);

        int minutes =
            Mathf.FloorToInt(time / 60);

        int seconds =
            Mathf.FloorToInt(time % 60);

        timerText.text =
            $"{minutes:00}:{seconds:00}";
    }

    public void Show(
        string challengeName,
        TimedPlatformChallenge challenge)
    {
        currentChallenge = challenge;

        titleText.text = challengeName;

        root.SetActive(true);
    }

    public void Hide()
    {
        currentChallenge = null;

        root.SetActive(false);
    }
}
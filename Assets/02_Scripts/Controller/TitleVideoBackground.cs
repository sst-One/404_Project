using UnityEngine;
using UnityEngine.Video;

public class TitleVideoBackground : MonoBehaviour
{
    [Header("Video Settings")]
    public VideoPlayer titleVideoPlayer;
    public GameObject videoUIContainer; // RawImage 오브젝트

    private void Start()
    {
        if (videoUIContainer != null)
        {
            videoUIContainer.SetActive(true);
        }

        if (titleVideoPlayer != null)
        {
            titleVideoPlayer.isLooping = true;
            titleVideoPlayer.Play();
        }
    }
}
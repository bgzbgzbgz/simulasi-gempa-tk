using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Memutar urutan frame Sprite pada komponen Image UI sehingga berfungsi seperti GIF animasi yang sangat ringan.
/// </summary>
[RequireComponent(typeof(Image))]
public class AnimatedImageUI : MonoBehaviour
{
    [Tooltip("Daftar frame animasi berurutan")]
    public Sprite[] frames;

    [Tooltip("Kecepatan frame per detik (FPS)")]
    public float fps = 11f;

    [Tooltip("Apakah animasi berputar terus-menerus (loop)?")]
    public bool loop = true;

    private Image targetImage;
    private int currentFrameIndex = 0;
    private float timer = 0f;

    private void Awake()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
    }

    private void OnEnable()
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
        timer = 0f;
        currentFrameIndex = 0;
        ApplyFrame();
    }

    private void Update()
    {
        if (frames == null || frames.Length <= 1) return;
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
            if (targetImage == null) return;
        }

        timer += Time.deltaTime;
        float frameDuration = 1f / Mathf.Max(1f, fps);

        if (timer >= frameDuration)
        {
            timer -= frameDuration;
            currentFrameIndex++;

            if (currentFrameIndex >= frames.Length)
            {
                if (loop)
                {
                    currentFrameIndex = 0;
                }
                else
                {
                    currentFrameIndex = frames.Length - 1;
                }
            }

            ApplyFrame();
        }
    }

    public void SetFrames(Sprite[] newFrames, float newFps = 11f)
    {
        if (targetImage == null)
        {
            targetImage = GetComponent<Image>();
        }
        frames = newFrames;
        fps = newFps;
        timer = 0f;
        currentFrameIndex = 0;
        ApplyFrame();
    }

    public void Clear()
    {
        frames = null;
        currentFrameIndex = 0;
    }

    private void ApplyFrame()
    {
        if (targetImage != null && frames != null && frames.Length > 0 && currentFrameIndex < frames.Length)
        {
            if (frames[currentFrameIndex] != null)
            {
                targetImage.sprite = frames[currentFrameIndex];
                targetImage.preserveAspect = true;
            }
        }
    }
}

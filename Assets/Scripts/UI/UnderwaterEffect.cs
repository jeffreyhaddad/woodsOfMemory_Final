using UnityEngine;

public class UnderWaterEffect : MonoBehaviour
{
    private Texture2D overlayTex;
    private Texture2D vigTopTex;
    private Texture2D vigBottomTex;
    private Texture2D vigLeftTex;
    private Texture2D vigRightTex;
    private float currentAlpha = 0f;
    public AudioClip underWaterSound;
    private Color normalFogColor;
    private float normalFogDensity;

    private AudioSource underwaterSource;

    private void Awake()
    {
        underwaterSource = gameObject.AddComponent<AudioSource>();
        underwaterSource.playOnAwake = false;
        underwaterSource.spatialBlend = 0f;
        underwaterSource.loop = true;
        underwaterSource.volume = 0.4f;
        underwaterSource.clip = underWaterSound;
        // Light blue fullscreen overlay
        overlayTex = new Texture2D(1, 1);
        overlayTex.SetPixel(0, 0, new Color(0f, 0.3f, 0.8f));
        overlayTex.Apply();

        int size = 64;
        Color vigColor = new Color(0f, 0.1f, 0.3f);

        // Each texture is a gradient: opaque at the screen edge, transparent toward center
        // Unity GUI maps texture y=0 (SetPixel bottom) to the bottom of the drawn Rect

        vigTopTex = new Texture2D(1, size);
        for (int y = 0; y < size; y++)
            vigTopTex.SetPixel(0, y, new Color(vigColor.r, vigColor.g, vigColor.b, y / (float)(size - 1)));
        vigTopTex.Apply();

        vigBottomTex = new Texture2D(1, size);
        for (int y = 0; y < size; y++)
            vigBottomTex.SetPixel(0, y, new Color(vigColor.r, vigColor.g, vigColor.b, 1f - y / (float)(size - 1)));
        vigBottomTex.Apply();

        vigLeftTex = new Texture2D(size, 1);
        for (int x = 0; x < size; x++)
            vigLeftTex.SetPixel(x, 0, new Color(vigColor.r, vigColor.g, vigColor.b, 1f - x / (float)(size - 1)));
        vigLeftTex.Apply();

        vigRightTex = new Texture2D(size, 1);
        for (int x = 0; x < size; x++)
            vigRightTex.SetPixel(x, 0, new Color(vigColor.r, vigColor.g, vigColor.b, x / (float)(size - 1)));
        vigRightTex.Apply();

        normalFogColor = RenderSettings.fogColor;
        normalFogDensity = RenderSettings.fogDensity;
    }

    void Update()
    {
        if (PlayerMovement.isInwater)
        {
            currentAlpha = 0.12f;
            RenderSettings.fogColor = new Color(0f, 0.2f, 0.5f);
            RenderSettings.fogDensity = 0.08f;
            if (!underwaterSource.isPlaying) underwaterSource.Play();
        }
        else
        {
            currentAlpha = 0f;
            RenderSettings.fogColor = normalFogColor;
            RenderSettings.fogDensity = normalFogDensity;
            if (underwaterSource.isPlaying) underwaterSource.Stop();
        }
    }

    private void OnGUI()
    {
        if (currentAlpha < 0.001f) return;

        // Light blue fullscreen tint
        GUI.color = new Color(1f, 1f, 1f, currentAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTex);

        // Gradient vignette — each edge fades from dark blue inward, they blend together in corners
        float thickness = 400f;
        GUI.color = Color.white;
        GUI.DrawTexture(new Rect(0, 0, Screen.width, thickness), vigTopTex);
        GUI.DrawTexture(new Rect(0, Screen.height - thickness, Screen.width, thickness), vigBottomTex);
        GUI.DrawTexture(new Rect(0, 0, thickness, Screen.height), vigLeftTex);
        GUI.DrawTexture(new Rect(Screen.width - thickness, 0, thickness, Screen.height), vigRightTex);

        GUI.color = Color.white;
    }
}

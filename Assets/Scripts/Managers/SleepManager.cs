using System.Collections;
using UnityEngine;

public class SleepManager : MonoBehaviour
{
    public static SleepManager Instance { get; private set; }

    [Header("Timings")]
    public float fadeOutDuration = 1.5f;
    public float holdDuration = 1f;
    public float fadeInDuration = 1.5f;

    private float blackAlpha = 0f;
    private Texture2D blackTex;
    private bool isSleeping = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        blackTex = new Texture2D(1, 1);
        blackTex.SetPixel(0, 0, Color.black);
        blackTex.Apply();
    }

    void OnGUI()
    {
        if (blackAlpha <= 0f) return;
        GUI.color = new Color(0f, 0f, 0f, blackAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), blackTex);
        GUI.color = Color.white;
    }

    public void StartSleep(Transform sleepAnchor)
    {
        if (isSleeping) return;
        StartCoroutine(SleepRoutine(sleepAnchor));
    }

    IEnumerator SleepRoutine(Transform sleepAnchor)
    {
        isSleeping = true;
        GameManager.Instance.SetState(GameState.Cutscene);

        // Get player components
        PlayerVitals vitals = GameManager.Instance.PlayerVitals;
        Animator anim = vitals.GetComponentInChildren<Animator>();
        CharacterController cc = vitals.GetComponent<CharacterController>();

        // Disable CharacterController so we can reposition the player freely
        if (cc != null) cc.enabled = false;

        // Snap player to the bed anchor position and rotation
        if (sleepAnchor != null)
        {
            vitals.transform.position = sleepAnchor.position;
            vitals.transform.rotation = sleepAnchor.rotation;
        }

        // Re-enable CharacterController
        if (cc != null) cc.enabled = true;

        // Trigger lay down animation (5.733s to complete)
        if (anim != null)
            anim.SetBool("isSleeping", true);

        yield return new WaitForSeconds(5.733f);

        // Hold briefly in sleep idle so player sees them lying on the bed
        yield return new WaitForSeconds(0.5f);

        // Fade to black
        float t = 0f;
        while (t < fadeOutDuration)
        {
            t += Time.deltaTime;
            blackAlpha = Mathf.Clamp01(t / fadeOutDuration);
            yield return null;
        }
        blackAlpha = 1f;

        // Advance time to dawn
        GameManager.Instance.DayNight.TimeOfDay = 0.25f;

        // Restore vitals
        if (vitals != null)
        {
            vitals.Health  = vitals.maxHealth;
            vitals.Hunger  = vitals.maxHunger * 0.6f;
            vitals.Stamina = vitals.maxStamina;
        }

        yield return new WaitForSeconds(holdDuration);

        // Trigger get up — starts while screen is still black
        if (anim != null)
            anim.SetBool("isSleeping", false);

        // Fade back in — player visibly gets up from the bed
        t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            blackAlpha = Mathf.Clamp01(1f - (t / fadeInDuration));
            yield return null;
        }
        blackAlpha = 0f;

        // Wait for the remainder of the get up animation (2.7 - 1.5 = 1.2s)
        yield return new WaitForSeconds(2.7f - fadeInDuration);

        GameManager.Instance.SetState(GameState.Playing);
        isSleeping = false;
    }
}

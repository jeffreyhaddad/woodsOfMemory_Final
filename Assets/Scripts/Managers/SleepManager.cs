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

    public bool IsNightTime()
    {
        float t = GameManager.Instance.DayNight.TimeOfDay;
        return t > 0.75f || t < 0.17f;
    }

    // wakeSidePosition: XZ position beside the bed (Y is ignored — we use the player's standing Y)
    public void StartSleep(Vector3 wakeSidePosition, Quaternion wakeRotation)
    {
        if (isSleeping) return;
        if (!IsNightTime()) return;
        StartCoroutine(SleepRoutine(wakeSidePosition, wakeRotation));
    }

    IEnumerator SleepRoutine(Vector3 wakeSidePosition, Quaternion wakeRotation)
    {
        isSleeping = true;
        GameManager.Instance.SetState(GameState.Cutscene);

        PlayerVitals vitals = GameManager.Instance.PlayerVitals;
        Animator anim = vitals.GetComponentInChildren<Animator>();
        CharacterController cc = vitals.GetComponent<CharacterController>();

        // Save the player's standing Y so we can restore ground level on wake
        float standingY = vitals.transform.position.y;

        // Play lay down animation in place — no teleport, player is already at the bed
        if (anim != null)
            anim.SetBool("isSleeping", true);

        yield return new WaitForSeconds(5.733f);
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

        // Advance time to dawn and restore vitals while screen is black
        GameManager.Instance.DayNight.TimeOfDay = 0.25f;

        if (vitals != null)
        {
            vitals.Health  = vitals.maxHealth;
            vitals.Hunger  = vitals.maxHunger * 0.6f;
            vitals.Stamina = vitals.maxStamina;
        }

        yield return new WaitForSeconds(holdDuration);

        // Teleport player to beside the bed, keeping original standing Y
        if (cc != null) cc.enabled = false;

        Vector3 wakePos = new Vector3(wakeSidePosition.x, standingY, wakeSidePosition.z);
        vitals.transform.position = wakePos;
        vitals.transform.rotation = wakeRotation;

        yield return null;
        yield return null;

        if (cc != null) cc.enabled = true;

        // Trigger get up animation
        if (anim != null)
            anim.SetBool("isSleeping", false);

        // Fade back in
        t = 0f;
        while (t < fadeInDuration)
        {
            t += Time.deltaTime;
            blackAlpha = Mathf.Clamp01(1f - (t / fadeInDuration));
            yield return null;
        }
        blackAlpha = 0f;

        yield return new WaitForSeconds(2.7f - fadeInDuration);

        GameManager.Instance.SetState(GameState.Playing);
        isSleeping = false;
    }
}

using UnityEngine;

public class GetKeyFromBoxScript : MonoBehaviour
{
    public GameObject spawnPrefab;

    GameObject player;
    GameObject woodenBox;
    DayNightCycle dayNight;
    bool isPlayerNear = false;
    bool hasbeenOpened = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        woodenBox = GameObject.FindGameObjectWithTag("WoodenBox");
        dayNight = FindAnyObjectByType<DayNightCycle>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = true;
            Debug.Log("Player entered box trigger");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            Debug.Log("Player exited box trigger");
        }
    }

    private void OnGUI()
    {
        if (isPlayerNear && !hasbeenOpened && MissionManager.Instance.CurrentMissionIndex == 2)
        {
           // if (dayNight != null && dayNight.IsNight)
                GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height - 100, 300, 30), "Press O to open the box");
            //else
                //GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height - 100, 300, 30), "This can only be opened at night");
        }
    }

    void Update()
    {
        if (MissionManager.Instance.CurrentMissionIndex == 2 && isPlayerNear && Input.GetKeyDown(KeyCode.O) && !hasbeenOpened)
        {
            hasbeenOpened = true;
            MissionManager.Instance.ReportLocationReached("wooden box");
            if (woodenBox != null && woodenBox != gameObject)
                Destroy(woodenBox);
            Destroy(gameObject);
            if (spawnPrefab != null)
                Instantiate(spawnPrefab, new Vector3(625.5f, 108f, 382f), Quaternion.identity);
        }
    }
}

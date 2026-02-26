using UnityEngine;

public class GetKeyFromBoxScript : MonoBehaviour
{
   
    
    GameObject player;
    GameObject woodenBox;
    GameObject rustedKey;
    
    DayNightCycle dayNight;
    bool isPlayerNear = false;
    bool hasbeenOpened = false;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        woodenBox = GameObject.FindGameObjectWithTag("WoodenBox");
        dayNight = FindAnyObjectByType<DayNightCycle>();
        rustedKey = GameObject.FindGameObjectWithTag("Rusted key");
        
       
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
           
                GUI.Label(new Rect(Screen.width / 2 - 150, Screen.height - 100, 300, 30), "Press O to open the box");
            
        }
        
    }

    void Update()
    {
        if (MissionManager.Instance.CurrentMissionIndex == 2 && isPlayerNear && Input.GetKeyDown(KeyCode.O) && !hasbeenOpened)
        {
            
            hasbeenOpened = true;
            MissionManager.Instance.ReportLocationReached("wooden box");
            
                
                Destroy(woodenBox);
                Destroy(gameObject);
                
            
        }
    }
}
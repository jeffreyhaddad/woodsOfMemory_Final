using UnityEngine;

public class SwimmableArea : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
           PlayerMovement pm = other.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                Debug.Log("Player Movement!");
                pm.gravity = -4f;
                pm.walkSpeed = 1f;
                pm.runSpeed = 2f;
                pm.runSpeed = 6f;
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement pm = other.GetComponent <PlayerMovement>();
            if (pm != null)
            {
                pm.gravity = -20f;
                pm.walkSpeed = 2f;
                pm.runSpeed = 5f;
                pm.runSpeed = 12f;

            }
            

        }

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

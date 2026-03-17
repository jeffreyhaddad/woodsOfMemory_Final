using UnityEngine;

public class SwimmableArea : MonoBehaviour
{

    
    
    private void OnTriggerEnter(Collider other)
    {
       
        if (other.CompareTag("Player"))
        
            PlayerMovement.isInwater = true;
          
            
        
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerMovement.isInwater = false;
            Debug.Log("Here !!");
        }
        

    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

using UnityEngine;

public class FinalBoneFireScript : MonoBehaviour
{
    Inventory inventory;
    public GameObject player;

    public bool isLit;
    public bool isPlayerInRange;

    private ParticleSystem fireEffect;
    private ParticleSystem smokeEffect;
  

    public void Start()
    {
        inventory = FindAnyObjectByType<Inventory>();

        Transform fireT  = transform.Find("FireEffect");
        Transform smokeT = transform.Find("SmokeEffect");
       

        if (fireT  != null) fireEffect  = fireT.GetComponent<ParticleSystem>();
        if (smokeT != null) smokeEffect = smokeT.GetComponent<ParticleSystem>();
        

        isLit = false;
    }
    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = true;
        }
    }
    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInRange = false;
        }
    }
    public void Update()
    {
        ItemData wood = ItemRegistry.Get("Wood");
        ItemData stone = ItemRegistry.Get("Stone");
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E) && inventory.HasItem(wood, 3) && inventory.HasItem(stone, 2) && !isLit)
        {
            if (fireEffect  != null) fireEffect.Play();
            if (smokeEffect != null) smokeEffect.Play();
            
            isLit = true;
        }
    }
}

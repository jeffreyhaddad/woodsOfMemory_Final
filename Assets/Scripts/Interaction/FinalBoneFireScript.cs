using Unity.VisualScripting;
using UnityEngine;

public class FinalBoneFireScript : MonoBehaviour
{

    Inventory inventory;
    public GameObject player;

    public ParticleSystem[] particuleSystems;
    public ParticleSystem fireEffect;
    public ParticleSystem smokeEffect;
    public ParticleSystem lightEffect;

    public bool isLit;

    public bool isPlayerInRange;

    public void Start()
    {
        inventory = FindAnyObjectByType<Inventory>();
        particuleSystems = GetComponentsInChildren<ParticleSystem>();
        fireEffect = particuleSystems[0];
        smokeEffect = particuleSystems[2];
        lightEffect = particuleSystems[1];
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
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E) && inventory.HasItem(wood, 3) && inventory.HasItem(stone,2) && !isLit)
        {
            fireEffect.Play();
            lightEffect.Play();
            smokeEffect.Play();
            isLit = true;

        }
    }
}

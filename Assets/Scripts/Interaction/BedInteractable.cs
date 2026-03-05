using UnityEngine;

public class BedInteractable : Interactable
{
    [SerializeField] private Transform sleepAnchor;

    private void Start()
    {
        promptText = "Sleep (Skip to Dawn)";
    }

    public override void OnInteract()
    {
        SleepManager.Instance.StartSleep(sleepAnchor);
    }
}

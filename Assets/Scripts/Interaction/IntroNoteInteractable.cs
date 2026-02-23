using UnityEngine;

/// <summary>
/// Interactable used for the cabin note at the start of the intro sequence.
/// Reading it hands control back to IntroSequenceManager.
/// </summary>
public class IntroNoteInteractable : Interactable
{
    public override void OnInteract()
    {
        // Don't allow the note to be read while the opening fade is still running.
        // (PlayerInteraction doesn't check inputBlocked, so E can be pressed
        //  while the screen is black — this prevents that from silently skipping
        //  the intro overlay.)
        if (IntroSequenceManager.Instance == null || !IntroSequenceManager.Instance.FadeComplete)
            return;

        IntroSequenceManager.Instance.OnNoteRead();
        Destroy(gameObject);
    }
}

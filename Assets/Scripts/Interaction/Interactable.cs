using UnityEngine;

/// <summary>
/// Base class for any object the player can interact with.
/// Inherit from this and override OnInteract() to define behavior.
/// </summary>
public abstract class Interactable : MonoBehaviour
{
    [Tooltip("Text shown in the interaction prompt (e.g. 'Pick up Firewood')")]
    public string promptText = "Interact";

    /// <summary>Called when the player presses E while looking at this object.</summary>
    public abstract void OnInteract();

    /// <summary>Called each frame the player is looking at this object.</summary>
    public virtual void OnFocus() { }

    /// <summary>Called when the player looks away from this object.</summary>
    public virtual void OnLoseFocus() { }
}

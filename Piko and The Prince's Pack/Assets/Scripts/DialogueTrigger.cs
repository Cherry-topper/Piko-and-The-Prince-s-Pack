using UnityEngine;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue")]
    [SerializeField] private DialogueLine[] lines;

    [Header("Trigger Settings")]
    [SerializeField] private bool playOnce = true;
    [SerializeField] private bool destroyAfterPlaying = false;

    private bool hasPlayed;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (hasPlayed && playOnce)
            return;

        if (!collision.CompareTag("Player"))
            return;

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("No DialogueManager found in the scene.");
            return;
        }

        DialogueManager.Instance.StartDialogue(lines);
        hasPlayed = true;

        if (destroyAfterPlaying)
        {
            Destroy(gameObject);
        }
    }
}
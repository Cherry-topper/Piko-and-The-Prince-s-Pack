using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private TMP_Text dialogueText;

    [Header("Settings")]
    [SerializeField] private KeyCode continueKey = KeyCode.E;
    [SerializeField] private float typingSpeed = 0.03f;

    private readonly Queue<DialogueLine> dialogueQueue = new Queue<DialogueLine>();

    private bool isDialogueActive;
    private bool isTyping;
    private string currentFullLine = "";
    private Coroutine typingCoroutine;

    public bool IsDialogueActive => isDialogueActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (!isDialogueActive)
            return;

        if (Input.GetKeyDown(continueKey))
        {
            if (isTyping)
                FinishTypingImmediately();
            else
                ShowNextLine();
        }
    }

    public void StartDialogue(DialogueLine[] lines)
    {
        if (isDialogueActive)
            return;

        if (lines == null || lines.Length == 0)
            return;

        if (dialoguePanel == null || speakerNameText == null || dialogueText == null)
        {
            Debug.LogWarning("DialogueManager is missing UI references.");
            return;
        }

        dialogueQueue.Clear();

        foreach (DialogueLine line in lines)
        {
            dialogueQueue.Enqueue(line);
        }

        isDialogueActive = true;
        dialoguePanel.SetActive(true);

        ShowNextLine();
    }

    private void ShowNextLine()
    {
        if (dialogueQueue.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = dialogueQueue.Dequeue();

        speakerNameText.text = line.speakerName;
        currentFullLine = line.dialogueText;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeLine(currentFullLine));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;
        dialogueText.text = "";

        foreach (char letter in line)
        {
            dialogueText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    private void FinishTypingImmediately()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        dialogueText.text = currentFullLine;
        isTyping = false;
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        dialoguePanel.SetActive(false);
        dialogueQueue.Clear();
    }
}
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;

    [TextArea(2, 4)]
    public string dialogueText;
}

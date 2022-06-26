using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTriggerOnInteract : DialogueTrigger
{
    protected virtual void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") && InputManager.Instance.GetInteractionPressed() && !DialogueManager.Instance.IsDialogueOn())
        {
            dialogue.startDialogue();
        }
    }
}   
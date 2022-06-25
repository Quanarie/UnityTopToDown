using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DialogueTriggerAndDiaryActivation : DialogueTriggerOnInteract
{
    private bool isDestroyed = false;

    protected override void Start()
    {
        base.Start();
        dialogue.OnEndOfDialogue += destroyDialogue;
        Diary.Instance.gameObject.SetActive(false);
    }

    protected override void OnTriggerStay2D(Collider2D collision)
    {
        if (InputManager.Instance.GetInteractionPressed())
        {
            if (isDestroyed)
            {
                if (collision.CompareTag("Player") && !Diary.Instance.IsDiaryOnScreen())
                {
                    Diary.Instance.Show();
                }
            }
            else
            {
                if (collision.CompareTag("Player") && !DialogueManager.Instance.IsDialogueOn())
                {
                    dialogue.startDialogue();
                }
            }
        }
    }

    private void destroyDialogue()
    {
        if (GetComponent<QuestIdentifier>().isCompletedAll())
        {
            isDestroyed = true;
            Diary.Instance.gameObject.SetActive(true);
            Diary.Instance.Show();
            Destroy(dialogue);
        }
    }
}

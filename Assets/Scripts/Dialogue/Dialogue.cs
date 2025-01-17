using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

public class Dialogue : MonoBehaviour
{
    public delegate void EndOfDialogue();
    public event EndOfDialogue OnEndOfDialogue;

    public delegate void ChoicesCreated();
    public event ChoicesCreated OnChoicesCreated;

    public delegate void ChoicePressed();
    public event ChoicePressed OnChoicePressed;

    public TextAsset story;

    private List<string> text;
    private int index;

    public bool isDialogueOn = false;
    private bool mustMoveForward = false;

    private TextMeshProUGUI dialogueText;

    private Stack<int> endsOfChoice = new();
    private Stack<int> endsOfOption = new();

    private const string choiceSign = "~";
    private const char choiceSignChar = '~';
    private const string pleasure = "Pleasure";
    private const string anxiety = "Anxiety";
    private const string realistic = "Realistic";
    private const string orSign = "|";
    private const string andSign = "&";

    public void Start()
    {
        dialogueText = DialogueManager.Instance.DialogueText.GetComponent<TextMeshProUGUI>();
        text = story.text.Split("\n").ToList();
        text = formatText(text);
    }

    private List<string> formatText(List<string> list)
    {
        for (int k = 0; k < list.Count; k++)
        {
            if (list[k].Contains(choiceSignChar) || list[k].Contains('*') || list[k].Contains('%') || list[k].Contains('/') || list[k].Contains('^'))
                continue;
            if (list[k] == "")
                continue;

            char[] chars = list[k].ToCharArray();
            list.Remove(list[k]);

            int j = 0;
            int lines = 0;
            string newLine = "";
            for (int i = 0; i < chars.Length; i++)
            {
                if (chars[i] == '#' || i == chars.Length - 1)
                {
                    int tempLines = Mathf.CeilToInt((i - j - 1) / DialogueManager.Instance.GetMaximumSymbolsInRow());
                    if (lines + tempLines <= DialogueManager.Instance.GetMaximumRows())
                    {
                        lines += tempLines;
                        for (; j < i; j++)
                        {
                            newLine += chars[j];
                        }
                    }
                    else
                    {
                        lines = tempLines;
                        list.Insert(k, newLine);
                        newLine = "";
                        j++;
                        for (; j < i; j++)
                        {
                            newLine += chars[j];
                        }
                        k++;
                    }
                }
            }
            list.Insert(k, newLine);
            k++;
        }
        return list;
    }

    private void Update()
    {
        if (DialogueManager.Instance.IsChoiceActive() || index >= text.Count || !isDialogueOn) return;

        if (!isLineEmpty(index) && isDialogueOn && endsOfChoice.Count == 0)
        {
            if (InputManager.Instance.GetInteractionPressed() || mustMoveForward)
            {
                mustMoveForward = false;
                readLine();
            }
        }

        if (endsOfOption.Count != 0)
        {
            if (index <= endsOfOption.Peek())
            {
                if (InputManager.Instance.GetInteractionPressed() || mustMoveForward)
                {
                    mustMoveForward = false;
                    readLine();
                }
            }
            else
            {
                index = endsOfChoice.Pop();
                index++;
                endsOfOption.Pop();
            }
        }

        if (isLineEmpty(index) && isDialogueOn)
        {
            if (InputManager.Instance.GetInteractionPressed() || mustMoveForward)
            {
                mustMoveForward = false;
                endDialogue();
            }
        }
    }

    private bool isLineEmpty(int ind)
    {
        return currentLine(ind) == "";
    }

    private void readLine()
    {
        if (firstChar(index) == '*')
        {
            displayChoices();
        }
        else if (firstChar(index) == '/')
        {
            string[] parameters = currentLine(index).Replace("/", "").Split(";");
            parameters[1] = parameters[1].Replace("+", "").Replace(choiceSign, "").Replace("*", "");
            changeScale(parameters[0], int.Parse(parameters[1]));
            mustMoveForward = true;

            if (!isLineEmpty(index) && isSpecial(firstChar(index))) readLine();
        }
        else if (firstChar(index) == '^')
        {
            string[] parameters = currentLine(index).Replace("^", "").Split(";");
            parameters[1] = parameters[1].Replace(choiceSign, "").Replace("*", "");
            changeStage(parameters[0], parameters[1]);
            mustMoveForward = true;

            if (!isLineEmpty(index) && isSpecial(firstChar(index))) readLine();
        }
        else if (firstChar(index) == '@')
        {
            Diary.Instance.AddAchievement(text[index].Replace("@", "").Replace("*", "").Replace(choiceSign, "").Trim());
            index++;

            if (!isLineEmpty(index) && isSpecial(firstChar(index))) readLine();
        }
        else outTextLine(index);
    } 

    private bool isSpecial(char c)
    {
        return c == '/' || c == '^' || c == '@';
    }

    private void displayChoices()
    {
        string[] line = currentLine(index).Split(";");
        line[0] = line[0].Replace("*", "");
        List<Button> choices = DialogueManager.Instance.ShowChoices(line.Length);

        for (int i = 0; i < choices.Count; i++)
        {
            choices[i].gameObject.SetActive(true);
            int capturedi = i;
            if (line[i].Contains('(') && line[i].Contains(')'))
            {
                string conditions = "";
                for (int j = 0; j < line[i].Length; j++)
                {
                    if (line[i][j] == '(')
                    {
                        for (int k = j + 1; k < line[i].Length - 1; k++)
                        {
                            conditions += line[i][k];
                        }
                        break;
                    }
                }
                string text = "";
                foreach(char c in line[i])
                {
                    if (c != '(') text += c;
                    else break;
                }
                line[i] = text;

                if (isChoiceActive(conditions))
                {
                    choices[i].onClick.AddListener(() => startChoices(line[capturedi]));
                }
            }
            else
            {
                choices[i].onClick.AddListener(() => startChoices(line[capturedi]));
            }

            choices[i].GetComponentInChildren<TextMeshProUGUI>().text = line[i];
        }

        DialogueManager.Instance.RandomizeChoices();
        OnChoicesCreated?.Invoke();

        index++; // from * to first &
        endsOfChoice.Push(findTheEndChoice(index));
    }

    private bool isChoiceActive(string disjConjText)
    {
        string[] conjParts = disjConjText.Split("|");
        for (int i = 0; i < conjParts.Length; i++)
        {
            bool isPartTrue = true;
            string[] conditions = conjParts[i].Split("&");
            for (int j = 0; j < conditions.Length; j++)
            {
                if (!CheckCondition(conditions[j].Replace("(", "").Replace(")", "")))
                {
                    isPartTrue = false;
                    break;
                }
            }

            if (isPartTrue) return true;
        }
        return false;
    }

    private bool CheckCondition(string condition)
    {
        if (condition.Split("<").Length > 1)
        {
            string[] splitted = condition.Split("<");
            return Scales.Instance.IsLessThen(splitted[0], splitted[1]);
        }
        else if (condition.Split(">").Length > 1)
        {
            string[] splitted = condition.Split(">");
            if (condition[0] == 'l')
            {
                return PlayerPrefs.GetInt("currentLevel") > int.Parse(splitted[1]);
            }

            return Scales.Instance.IsBiggerThen(splitted[0], splitted[1]);
        }
        else
        {
            string[] splitted = condition.Replace("q", "").Replace(":", "").Replace("(", "").Replace(")", "").Split("/");
            int id = int.Parse(splitted[0]);
            int stage = int.Parse(splitted[1]);
            return QuestManager.Instance.GetStage(id) == stage;
        }
    }

    private void startChoices(string choice)
    {
        OnChoicePressed?.Invoke();
        DialogueManager.Instance.HideChoices();
        reactToChoice(choice);
        readLine();
    }

    private void reactToChoice(string choice)
    {
        if (currentLine(index).Replace(choiceSign, "") == choice)
        {
            int endOfOption = findTheEndOption(index);
            endsOfOption.Push(endOfOption);
            index++;
        }
        else
        {
            index = findTheEndOption(index);
            index++; // after option
            reactToChoice(choice);
        }
    }

    private int findTheEndChoice(int ind)
    {
        int i = 0;
        while (i < 1)
        {
            ind++;
            if (firstChar(ind) == '*') i--;
            else if (lastChar(ind) == '*' || lastChar(ind) == choiceSignChar)
            {
                for (int j = currentLine(ind).Length - 1; j > 0; j--)
                {
                    if (currentLine(ind).ToCharArray()[j] == '*')
                    {
                        i++;
                    }
                    else if (currentLine(ind).ToCharArray()[j] != choiceSignChar)
                    {
                        break;
                    }
                }
            }
        }
        return ind;
    }

    private int findTheEndOption(int ind)
    {
        int i = 0;
        while (i < 1)
        {
            ind++;
            if (firstChar(ind) == choiceSignChar) i--;
            else if (lastChar(ind) == '*' || lastChar(ind) == choiceSignChar)
            {
                for (int j = currentLine(ind).Length - 1; j > 0; j--)
                {
                    if (currentLine(ind).ToCharArray()[j] == choiceSignChar)
                    {
                        i++;
                    }
                    else if (currentLine(ind).ToCharArray()[j] != '*')
                    {
                        break;
                    }
                }
            }
        }
        return ind;
    }

    private string currentLine(int ind)
    {
        return text[ind].TrimStart('\t').TrimEnd();
    }

    private void changeStage(string questId, string result)
    {
        if ((questId[0] == '+') || (questId[0] == '-'))
        {
            QuestManager.Instance.ChangeStage(int.Parse(questId), int.Parse(result));
        }
        else
        {
            QuestManager.Instance.SetStage(int.Parse(questId), int.Parse(result));
        }

        index++;
    }

    private void changeScale(string name, int delta)
    {
        if (name == pleasure)
        {
            Scales.Instance.AddPleasure(delta);
        }
        else if (name == anxiety)
        {
            Scales.Instance.AddAnxiety(delta);
        }
        else if (name == realistic)
        {
            Scales.Instance.AddRealistic(delta);
        }
        index++;
    }

    private void outTextLine(int ind)
    {
        dialogueText.text = text[ind].Trim().Replace("*", "").Replace(choiceSign, "").Replace("#", "\n");
        if (dialogueText.text == "")
        {
            mustMoveForward = true;
        }
        index++;
    }

    private char firstChar(int ind)
    {
        return currentLine(ind).ToCharArray()[0];
    }

    private char lastChar(int ind)
    {
        return currentLine(ind).ToCharArray()[currentLine(ind).Length - 1];
    }

    public void startDialogue()
    {
        int temp = chooseDialogue();
        if (temp != -1)
        {
            isDialogueOn = true;
            DialogueManager.Instance.Show();
            index = temp + 1;
            readLine();
        }
    }

    public void startDialogue(string situationName) // for situations
    {
        isDialogueOn = true;
        index = 0;
        reactToDialogueStart(situationName);
    }

    private void reactToDialogueStart(string dialogueId)
    {
        if (currentLine(index).Replace("%", "") == dialogueId)
        {
            DialogueManager.Instance.Show();
            index++;
            readLine();
        }
        else
        {
            index = findTheEndDialogue(index);
            index++;
            if (index < text.Count)
            {
                reactToDialogueStart(dialogueId);
            }
            else
            {
                Debug.LogWarning("Did not find a dialogue for " + gameObject.name);
            }
        }
    }

    public int isThereAQuestDialogue()
    {
        for (int i = 0; i < text.Count; i++)
        {
            if (currentLine(i).Contains("%"))
            {
                string[] splitted = currentLine(i).Split(";");
                if (splitted[0] == "%quest")
                {
                    string[] quests = splitted[1].Split(orSign);
                    string[] stages = splitted[2].Split(orSign);
                    for (int j = 0; j < quests.Length; j++)
                    {
                        if (checkQuests(quests[j].Replace("(", "").Replace(")", ""), stages[j].Replace("(", "").Replace(")", "")))
                        {
                            return int.Parse(quests[j]);
                        }
                    }
                }
            }
        }
        return -1;
    }

    public int chooseDialogue()
    {
        int standartIndex = -1;
        for (int i = 0; i < text.Count; i++)
        {
            if (currentLine(i).Contains("%"))
            {
                string[] splitted = currentLine(i).Split(";");
                if (splitted[0] == "%quest")
                {
                    string[] quests = splitted[1].Split(orSign);
                    string[] stages = splitted[2].Split(orSign);
                    for (int j = 0; j < quests.Length; j++)
                    {
                        if (checkQuests(quests[j].Replace("(", "").Replace(")", ""), stages[j].Replace("(", "").Replace(")", "")))
                        {
                            return i;
                        }
                    }
                }
                else
                {
                    standartIndex = i;
                }
            }
        }
        if (standartIndex != -1)
        {
            return standartIndex;
        }

        return -1;
    }

    private bool checkQuests(string ids, string stages)
    {
        string[] eachId = ids.Split(andSign);
        string[] eachStage = stages.Split(andSign);

        for (int i = 0; i < eachId.Length; i++)
        {
            if (QuestManager.Instance.GetStage(int.Parse(eachId[i])) != int.Parse(eachStage[i]))
            {
                return false;
            }
        }
        return true;
    }

    private int findTheEndDialogue(int ind)
    {
        while (!isLineEmpty(ind))
        {
            ind++;
        }
        return ind;
    }

    public void endDialogue()
    {
        DialogueManager.Instance.Hide();
        isDialogueOn = false;
        OnEndOfDialogue?.Invoke();
    }
}
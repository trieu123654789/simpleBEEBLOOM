using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

public class ConversationUIController : MonoBehaviour
{
    [SerializeField] private float _typeSpeed;

    [Header("UI Components")]
    [SerializeField] private GameObject _dialoguePanel;
    [SerializeField] private Text _dialogueText;
    [SerializeField] private Text _npcNameText;

    private StringBuilder stringBuilder = new StringBuilder();
    private bool _isTyping = false;
    private int _currentLine;
    private List<string> _npcDialogues;
    private GameObject _talkButton;


    public void StartConversation(string npcName, List<string> dialogues)
    {
        // hide npc talk button
        _talkButton?.SetActive(false);

        _npcDialogues = dialogues;

        _dialogueText.text = "";
        _npcNameText.text = npcName;

        stringBuilder.Clear();
        _currentLine = 0;

        EnableUI(true);
        StartCoroutine(PlayDialogue());
    }


    private IEnumerator PlayDialogue()
    {
        _isTyping = true;

        _dialogueText.text = "";
        stringBuilder.Clear();

        foreach (var letter in _npcDialogues[_currentLine])
        {
            // add letter to dialogue ui
            stringBuilder.Append(letter);
            _dialogueText.text = stringBuilder.ToString();

            yield return new WaitForSeconds(_typeSpeed);
        }

        _isTyping = false;
    }


    public void PlayNextLine()
    {
        // if the dialogue is typing, skip typing, display full dialogue
        bool skipTyping = false;
        
        if (_isTyping)
        {
            StopAllCoroutines();
            _dialogueText.text = _npcDialogues[_currentLine];

            skipTyping = true;
            _isTyping = false;
        }

        // if player skip typing, return
        if (skipTyping) return;

        // if dialogue is typed completely, play  next line
        if (++_currentLine < _npcDialogues.Count)
        {
            StopAllCoroutines();
            StartCoroutine(PlayDialogue());
        }
        else
        {
            EndConversation();
        }
    }


    public void EndConversation()
    {
        StopAllCoroutines();
        EnableUI(false);
    }


    private void EnableUI(bool enable)
    {
        _dialoguePanel.SetActive(enable);
    }
}
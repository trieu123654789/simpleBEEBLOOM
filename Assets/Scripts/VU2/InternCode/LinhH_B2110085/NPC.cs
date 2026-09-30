using UnityEngine;
using System.IO;
using System.Reflection;

public class NPC : MonoBehaviour
{
    [SerializeField] private string _npcId;
    [SerializeField] private ConversationUIController _conversationController;
    [SerializeField] private Thuan_23127_JsonReader _jsonReader;

    private NPCDialogue _npcDialogues = null;
    private string _fileName = "data";
    private Root _root;

    private string _currentLang = "vi";


    public void Talk()
    {
        GetDialoguesFromData();

        if (_npcDialogues != null)
        {
            _conversationController.StartConversation(_npcDialogues.npcName, _npcDialogues.dialogues);
        }
        else
            Debug.LogError("Can't get npc Id from data file.");
    }


    private void GetDialoguesFromData()
    {
        // get data file from Resources
        string resourceName = Path.GetFileNameWithoutExtension(_fileName);
        TextAsset jsonFile = Resources.Load<TextAsset>(resourceName);
        if (jsonFile == null)
        {
            Debug.LogError($"Không tìm thấy file JSON trong Resources: {resourceName}");
            return;
        }

        string jsonString = jsonFile.text;
        _root = JsonUtility.FromJson<Root>(jsonString);

        // get current langue (?)
        var lang = _jsonReader.GetCurrentLangData();

        // get npc dialogues via npc id
        var dialogues = lang?.npcDialogues;
        _npcDialogues = dialogues.Find(npc => npc.npcId == _npcId);
    }
}
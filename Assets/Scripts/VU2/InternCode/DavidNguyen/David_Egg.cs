using UnityEngine;

public class David_Egg : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("Points awarded when collected")]
    public int pointValue = 3;
    
    [Tooltip("Sound played when collected")]
    public AudioClip collectSound;
    
    private void Start()
    {
        var fruitComponent = GetComponent<David_Fruit>();
        if(!fruitComponent)
        {
            Debug.LogWarning("Please add David_Fruit component and set fruitType = Egg");
        }
    }
}

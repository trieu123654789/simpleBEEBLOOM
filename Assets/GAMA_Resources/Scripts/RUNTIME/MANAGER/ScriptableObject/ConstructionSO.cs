using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Construction1", menuName = "ScriptableObjects/ConstructionSO")]
public class ConstructionSO : ScriptableObject
{
    //public string name;
    public GameObject modelBuildPrefab;
    public GameObject finalPrefab;
    public int cost;
    [TextAreaAttribute]
    public string description;
    public int maxQuantity;
    public float cooldownTime;

    [Header("Carry-and-Plant Mode (vd: trồng cây)")]
    [Tooltip("Nếu bật: sau khi nhấn Button, một bản sao 'cây con' sẽ xuất hiện trên tay người chơi. Người chơi phải đi vào PlantingZone rồi mới đặt được công trình. Nếu tắt: dùng flow ray + SurfaceConnector mặc định.")]
    public bool requireCarryToPlant = false;
    [Tooltip("Prefab hiển thị trên tay khi đang mang (vd: cây con). Nếu để trống sẽ dùng modelBuildPrefab.")]
    public GameObject carryPrefab;
    [Tooltip("Scale tương đối khi cầm trên tay (so với prefab gốc)")]
    public float carryScale = 0.3f;

    private int currentQuantity;
    private float currentTime;

    public int CurrentQuantity
    {
        get { return currentQuantity; }
    }

    public float CurrentTime
    {
        get { return currentTime; }
    }


    public void Init()
    {
        currentQuantity = maxQuantity;
        currentTime = 0;
    }

    public void ResetCooldown()
    {
        currentTime = cooldownTime;
    }

    public void DecreaseQuantity()
    {
        if(currentQuantity > 0)
        {
            currentQuantity--;
        }
    }

    public void IncreaseQuantity()
    {
        if(currentQuantity < maxQuantity)
        {
            currentQuantity++;
        }
    }

    public void DecreaseCooldown(float deltaTime)
    {
        if(currentTime > 0)
        {
            currentTime -= deltaTime;
        }
    }
}

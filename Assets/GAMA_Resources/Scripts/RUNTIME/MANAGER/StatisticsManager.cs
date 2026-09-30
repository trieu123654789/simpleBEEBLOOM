using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ActionHistory
{
    public string datetime;
    public string action;
    public string construction;
    public Vector2 position;

    public ActionHistory(string action, string construction, Vector2 position)
    {
        datetime = DateTime.Now.ToString("M/d/yyyy hh:mm:ss");
        this.action = action;
        this.construction = construction;
        this.position = position;
    }
}

public class StatisticsManager : MonoBehaviour
{
    /* 
    Statistics Manager: (v) -> Quản lý thống kê
    Manage and compile final data information of objects in the game screen.

    ----------------------------------
    Message By Hồng Sơn: 
    We are processing information adjustments to accommodate educational programs.

     */
    public static StatisticsManager Instance = null;

    private int currentLakeCount = 0;
    private int currentWaterPumpCount = 0;
    private int currentSluiceGateCount = 0;
    private int currentTreeBarrierCount = 0;
    private int currentEnemyCount = 0;
    // Số enemy mặn đã xâm nhập tới cuối tuyến (không bị diệt, không bị chặn).
    // Number of saltwater enemies that reached the end of their path (breached inland).
    private int currentEnemyBreachedCount = 0;
    // Số cây ăn quả/lương thực chết (Tree.cs, David_DurianTree, David_Rice...).
    // Number of fruit/crop trees that died (excluding planted TreeBarrier forest trees).
    private int currentFruitTreeDeathCount = 0;
    
    [HideInInspector] public List<ActionHistory> histories;

    private void Start()
    {
        histories = new List<ActionHistory>();
    }

    private void Awake()
    {
        Instance = this;
    }

    public void IncreateLakeCount() 
    {
        currentLakeCount += 1;
    }

    public void IncreateWaterPumpCount()
    {
        currentWaterPumpCount += 1;
    }
    public void IncreateSluiceGateCount()
    {
        currentSluiceGateCount += 1;
    }

    public void IncreateTreeBarrierCount()
    {
        currentTreeBarrierCount += 1;
    }

    public void IncreaseEnemyKillCount()
    {
        currentEnemyCount += 1;
    }

    // Gọi khi 1 enemy mặn đi hết tuyến waypoint mà chưa bị diệt → coi như xâm nhập nội đồng.
    // Called when an enemy reaches the end of its waypoint path without being killed.
    public void IncreaseEnemyBreachedCount()
    {
        currentEnemyBreachedCount += 1;
    }

    // Gọi khi 1 cây ăn quả/lương thực (Tree.cs, David_DurianTree, David_Rice...) chết.
    // Called when a fruit/crop tree dies.
    public void IncreaseFruitTreeDeathCount()
    {
        currentFruitTreeDeathCount += 1;
    }

    public void AddActionHistory(string action, string construction, Vector2 position)
    {
        histories.Add(
            new ActionHistory(action,construction,position)
        );
        Debug.Log(histories[histories.Count-1].datetime);
        Debug.Log(histories[histories.Count-1].action);
        Debug.Log(histories[histories.Count-1].construction);
        Debug.Log(histories[histories.Count-1].position);
    }


    //Getter
    public int LakeCount
    {
        get { return currentLakeCount; }
    }

    public int WaterPumpCount
    {
        get { return currentWaterPumpCount; }
    }

    public int SluiceGateCount
    {
        get { return currentSluiceGateCount; }
    }

    public int TreeBarrierCount
    {
        get { return currentTreeBarrierCount; }
    }

    /*
    Trung hòa được bao nhiêu con nước
     */
    public int EnemyKillCount
    { 
        get { return currentEnemyCount; } 
    }

    // Số enemy xâm nhập nội đồng (đi hết tuyến mà chưa bị diệt).
    public int EnemyBreachedCount
    {
        get { return currentEnemyBreachedCount; }
    }

    // Số cây ăn quả / lương thực đã chết.
    public int FruitTreeDeathCount
    {
        get { return currentFruitTreeDeathCount; }
    }
}

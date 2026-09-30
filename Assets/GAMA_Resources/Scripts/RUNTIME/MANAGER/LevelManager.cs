using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public enum WaveStep
{
    Preparation,
    Defense
}

public class LevelManager : MonoBehaviour
{
    /*
    Managing game levels
    Currently has 2 stages on the same level
    - Preparation (Rainy Season): 60s - rain effects active, no saltwater intrusion
    - Defense: enemies spawn, saltwater intrusion begins
     */
    [SerializeField] private bool loop = false;
    [SerializeField] private List<WaveSO> waves;
    [SerializeField] private List<EnemySpawner> spawns;

    [Header("Rainy Season (Preparation Phase)")]
    [SerializeField] private GameObject rainEffect;
    [SerializeField] private GameObject rainIcon;
    [SerializeField] private GameObject sunIcon;

    // Static event: fired when wave step changes (Preparation ↔ Defense)
    public static event System.Action<WaveStep> OnWaveStepChanged;

    // runtime privates
    private bool finished = false;
    private WaveStep currentWaveStep;
    private int currentWave;
    private float currentTime;
    private int currentWaveSpawnIndex;


    public void setWaveTime(int prepTime, int defTime)
    {
        foreach(WaveSO w in waves)
        {
            w.preparationTime = 0.0f + prepTime;
            w.waveTime = 0.0f + defTime;

        }
    }
    // Getters
    public List<EnemySpawner> Spawns
    {
        get { return spawns; }
    }
    // Getters
    public bool Finished
    {
        get { return finished; }
    }
    public int CurrentWave
    {
        get { return currentWave + 1; }
    }
    public int MaxWave
    {
        get { return waves.Count; }
    }
    public float CurrentTime
    {
        get { return currentTime; }
    }
    public WaveStep CurrentWaveStep
    {
        get { return currentWaveStep; }
    }

    public float CurrentWaveStepTime
    {
        get
        {
            if (currentWaveStep == WaveStep.Preparation)
                return waves[currentWave].preparationTime;
            else
                return waves[currentWave].waveTime;
        }
    }

    public void ToggleLoop()
    {
        loop = !loop;
    }

    void Awake()
    {
        InitLevel();
    }

    void Update()
    {
        if (finished) return;


        if (currentTime == waves[currentWave].preparationTime && GameUI.Instance != null)
        {
            if (currentWave > 0)
            {
                GameUI.Instance.DeletePlayer(spawns[0].gameObject);
            }

            for (int i = currentWaveSpawnIndex; i < waves[currentWave].waveSpawns.Count; i++)
            {
                int spawnPointIndex = waves[currentWave].waveSpawns[i].spawnPoint - 1;

                if (GameUI.Instance != null && gameObject != null)
                {
                    GameUI.Instance.UpdateConstructionPosition(spawns[spawnPointIndex].gameObject);
                }
            }
        }
        currentTime -= Time.deltaTime;

        // Rainy season phase: activate rain effects, no saltwater intrusion
        if (currentWaveStep == WaveStep.Preparation)
        {
            SetRainActive(true);
            RulesOfTheGame_VU2_2.Saltwater_Intrusion = 0f;
        }

        if (currentTime <= 0)
        {
            if (!MoveToDefenseStep())
                MoveToNextWave();
        }
        else
        {
            ExecuteDefenseStep();
        }
    }

    void InitLevel()
    {
        currentWaveStep = WaveStep.Preparation;
        currentWave = 0;
        currentTime = waves[0].preparationTime;
        currentWaveSpawnIndex = 0;
        OnWaveStepChanged?.Invoke(WaveStep.Preparation);
    }

    bool MoveToDefenseStep()
    {
        if (currentWaveStep == WaveStep.Preparation)
        {
            currentTime = waves[currentWave].waveTime;
            currentWaveStep = WaveStep.Defense;

            // End rainy season: deactivate rain, saltwater intrusion begins
            SetRainActive(false);
            OnWaveStepChanged?.Invoke(WaveStep.Defense);

            return true;
        }
        return false;
    }

    bool MoveToNextWave()
    {
        if (currentWaveStep == WaveStep.Defense)
        {
            currentWave++;
            if (currentWave > waves.Count - 1)
            {
                if (loop) InitLevel();
                else
                {
                    finished = true;
                    SimulationManager.Instance.SendEndMessageToGAMA();
                }

            }
            else
            {
                currentWaveStep = WaveStep.Preparation;
                currentTime = waves[currentWave].preparationTime;
                currentWaveSpawnIndex = 0;
                OnWaveStepChanged?.Invoke(WaveStep.Preparation);
            }
            return true;
        }
        return false;
    }

    void ExecuteDefenseStep()
    {
        if (currentWaveStep != WaveStep.Defense) return;

        int start = currentWaveSpawnIndex;
        for (int i = start; i < waves[currentWave].waveSpawns.Count; i++)
        {
            float spawnTimeFrame = waves[currentWave].waveSpawns[i].timeFrame;
            if (waves[currentWave].waveTime - currentTime >= spawnTimeFrame)
            {
                int spawnPointIndex = waves[currentWave].waveSpawns[i].spawnPoint - 1;
                spawns[spawnPointIndex].StartAutoSpawn(
                    waves[currentWave].waveSpawns[i].spawnEnemy.gameObject,
                    waves[currentWave].waveSpawns[i].spawnAmount
                );
                currentWaveSpawnIndex = i + 1;
            }
            else
            {
                return;
            }
        }
    }

    /// <summary>
    /// Activates/deactivates rain effects and UI icons for the rainy season (Preparation phase).
    /// </summary>
    void SetRainActive(bool active)
    {
        if (rainEffect) rainEffect.SetActive(active);
        if (rainIcon) rainIcon.SetActive(active);
        if (sunIcon) sunIcon.SetActive(!active);
    }
}
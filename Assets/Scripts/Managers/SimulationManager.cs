using UnityEngine;

public class SimulationManager : MonoBehaviour
{
    public static SimulationManager Instance;

    [Header("Control Mode")]
    public bool useSimulatedPlayer = false;

    [Header("Simulation Speed")]
    public bool fastSimulation = false;
    public float manualTimeScale = 1f;
    public float simulationTimeScale = 5f;

    [Header("Visual Optimisation")]
    public bool skipSpellVisualsInSimulation = true;

    [Header("Enemy Turn Delay")]
    public float manualEnemyTurnDelay = 0.2f;
    public float simulationEnemyTurnDelay = 0f;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        ApplySimulationSettings();
    }

    public void ApplySimulationSettings()
    {
        if (useSimulatedPlayer && fastSimulation)
            Time.timeScale = simulationTimeScale;
        else
            Time.timeScale = manualTimeScale;
    }

    public bool IsUsingSimulatedPlayer()
    {
        return useSimulatedPlayer;
    }

    public bool ShouldSkipSpellVisuals()
    {
        return useSimulatedPlayer && skipSpellVisualsInSimulation;
    }

    public float GetEnemyTurnDelay()
    {
        if (useSimulatedPlayer && fastSimulation)
            return simulationEnemyTurnDelay;

        return manualEnemyTurnDelay;
    }
}
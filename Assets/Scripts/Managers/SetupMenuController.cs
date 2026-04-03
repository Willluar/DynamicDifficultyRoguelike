using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SetupMenuController : MonoBehaviour
{
    [Header("Menu Root")]
    public GameObject menuPanel;

    [Header("Mode Options")]
    public Toggle simulatedPlayerToggle;
    public Toggle fastSimulationToggle;
    public Toggle ddaToggle;
    public Toggle batchRunsToggle;

    [Header("Run Count")]
    public TMP_InputField runCountInput;

    [Header("Optional Status Text")]
    public TextMeshProUGUI statusText;

    private void Start()
    {
        if (menuPanel != null)
            menuPanel.SetActive(true);

        if (runCountInput != null && string.IsNullOrWhiteSpace(runCountInput.text))
            runCountInput.text = "10";

        UpdateStatusText();
    }

    public void StartButtonPressed()
    {
        ApplySettingsToManagers();

        if (menuPanel != null)
            menuPanel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.StartRun();
    }

    public void ApplySettingsToManagers()
    {
        bool useSimulatedPlayer = simulatedPlayerToggle != null && simulatedPlayerToggle.isOn;
        bool fastSimulation = fastSimulationToggle != null && fastSimulationToggle.isOn;
        bool useDDA = ddaToggle != null && ddaToggle.isOn;
        bool enableBatchRuns = batchRunsToggle != null && batchRunsToggle.isOn;

        int runCount = 1;
        if (runCountInput != null && !string.IsNullOrWhiteSpace(runCountInput.text))
            int.TryParse(runCountInput.text, out runCount);

        runCount = Mathf.Max(1, runCount);

        if (SimulationManager.Instance != null)
        {
            SimulationManager.Instance.useSimulatedPlayer = useSimulatedPlayer;
            SimulationManager.Instance.fastSimulation = fastSimulation;
            SimulationManager.Instance.ApplySimulationSettings();
        }

        if (GameManager.Instance != null)
            GameManager.Instance.useDDA = useDDA;

        if (MultiRunBatchController.Instance != null)
        {
            MultiRunBatchController.Instance.enableBatchRuns = enableBatchRuns;
            MultiRunBatchController.Instance.totalRunsToExecute = runCount;
            MultiRunBatchController.Instance.ResetBatchProgress();
        }

        UpdateStatusText();
    }

    public void UpdateStatusText()
    {
        if (statusText == null)
            return;

        string simulated = simulatedPlayerToggle != null && simulatedPlayerToggle.isOn ? "Simulated" : "Player";
        string fast = fastSimulationToggle != null && fastSimulationToggle.isOn ? "Fast" : "Normal";
        string dda = ddaToggle != null && ddaToggle.isOn ? "On" : "Off";
        string batch = batchRunsToggle != null && batchRunsToggle.isOn ? "On" : "Off";

        int runCount = 1;
        if (runCountInput != null && !string.IsNullOrWhiteSpace(runCountInput.text))
            int.TryParse(runCountInput.text, out runCount);

        runCount = Mathf.Max(1, runCount);

        statusText.text =
            "Control: " + simulated + "\n" +
            "Speed: " + fast + "\n" +
            "DDA: " + dda + "\n" +
            "Batch Runs: " + batch + "\n" +
            "Run Count: " + runCount;
    }
}
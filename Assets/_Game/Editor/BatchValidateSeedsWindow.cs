using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using MurinoHDR.Core;
using MurinoHDR.Generation;
using UnityEditor;
using UnityEngine;

namespace MurinoHDR.Editor
{

public sealed class BatchValidateSeedsWindow : EditorWindow
{
    private const string EnvStartSeed = "MURINO_BATCH_START_SEED";
    private const string EnvCount = "MURINO_BATCH_COUNT";
    private const string EnvMaxAttempts = "MURINO_BATCH_MAX_ATTEMPTS";
    private const string EnvStyle = "MURINO_BATCH_STYLE";
    private const string EnvOutputName = "MURINO_BATCH_OUTPUT";
    private const string EnvMinSuccessRate = "MURINO_BATCH_MIN_SUCCESS_RATE";
    private const string EnvMaxAllowedFailures = "MURINO_BATCH_MAX_ALLOWED_FAILURES";

    [Serializable]
    private sealed class BatchValidationEntry
    {
        public int Seed;
        public int AttemptsUsed;
        public bool Success;
        public string Style = string.Empty;
        public int Rooms;
        public int Doors;
        public int Windows;
        public int Pickups;
        public int Props;
        public float FastPathSeconds;
        public float LootPathSeconds;
        public string TopErrors = string.Empty;
        public string Warnings = string.Empty;
    }

    [Serializable]
    private sealed class BatchValidationSummary
    {
        public string GeneratedAt = string.Empty;
        public int StartSeed;
        public int Count;
        public int MaxAttempts;
        public bool UseStyleOverride;
        public string StyleOverride = string.Empty;
        public float SuccessRate;
        public float AverageAttempts;
        public List<BatchValidationEntry> Entries = new List<BatchValidationEntry>();
        public List<string> TopFailCodes = new List<string>();
    }

    private int _startSeed = 1000;
    private int _count = 50;
    private int _maxAttempts = 5;
    private bool _useStyleOverride;
    private FloorStyle _styleOverride = FloorStyle.CabinetHeavy;
    private string _outputName = "batch_validation";
    private Vector2 _scroll;
    private string _lastSummary = string.Empty;

    private sealed class BatchRunSettings
    {
        public int StartSeed;
        public int Count;
        public int MaxAttempts;
        public bool UseStyleOverride;
        public FloorStyle StyleOverride;
        public string OutputName = "batch_validation";
    }

    private sealed class BatchRunResult
    {
        public BatchValidationSummary Summary;
        public string CsvPath = string.Empty;
        public string JsonPath = string.Empty;
        public string SummaryText = string.Empty;
    }

    [MenuItem("Tools/Murino/Batch Validate Seeds...")]
    public static void Open()
    {
        var window = GetWindow<BatchValidateSeedsWindow>("Batch Validate Seeds");
        window.minSize = new Vector2(480f, 320f);
    }

    public static void RunBatchValidationFromCommandLine()
    {
        try
        {
            var settings = new BatchRunSettings();
            settings.StartSeed = ParseIntEnv(EnvStartSeed, 1000);
            settings.Count = Mathf.Max(1, ParseIntEnv(EnvCount, 100));
            settings.MaxAttempts = Mathf.Max(1, ParseIntEnv(EnvMaxAttempts, 5));
            settings.OutputName = ParseStringEnv(EnvOutputName, "batch_validation_cli");

            var styleValue = ParseStringEnv(EnvStyle, string.Empty);
            FloorStyle parsedStyle;
            if (!string.IsNullOrWhiteSpace(styleValue) && Enum.TryParse(styleValue, true, out parsedStyle))
            {
                settings.UseStyleOverride = true;
                settings.StyleOverride = parsedStyle;
            }

            var minSuccessRate = Mathf.Clamp01(ParseFloatEnv(EnvMinSuccessRate, 1f));
            var maxAllowedFailures = Mathf.Max(0, ParseIntEnv(EnvMaxAllowedFailures, 0));

            var runResult = RunBatch(settings);
            Debug.Log(string.Format("[GEN] CLI batch validation finished: {0}", runResult.SummaryText));

            var failureCount = 0;
            if (runResult.Summary != null)
            {
                for (var i = 0; i < runResult.Summary.Entries.Count; i++)
                {
                    if (!runResult.Summary.Entries[i].Success)
                    {
                        failureCount++;
                    }
                }
            }

            var successRate = runResult.Summary != null ? runResult.Summary.SuccessRate : 0f;
            var passed = successRate >= minSuccessRate && failureCount <= maxAllowedFailures;
            if (!passed)
            {
                Debug.LogError(string.Format(
                    "[GEN] Batch validation gate failed. SuccessRate={0:P2}, MinSuccessRate={1:P2}, Failures={2}, MaxAllowedFailures={3}, CSV={4}, JSON={5}",
                    successRate,
                    minSuccessRate,
                    failureCount,
                    maxAllowedFailures,
                    runResult.CsvPath,
                    runResult.JsonPath));
                EditorApplication.Exit(1);
                return;
            }

            EditorApplication.Exit(0);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);
        EditorGUILayout.LabelField("Murino Batch Validation", EditorStyles.boldLabel);
        _startSeed = EditorGUILayout.IntField("Start Seed", _startSeed);
        _count = Mathf.Max(1, EditorGUILayout.IntField("Count", _count));
        _maxAttempts = Mathf.Max(1, EditorGUILayout.IntField("Max Attempts", _maxAttempts));
        _useStyleOverride = EditorGUILayout.Toggle("Override Style", _useStyleOverride);
        using (new EditorGUI.DisabledScope(!_useStyleOverride))
        {
            _styleOverride = (FloorStyle)EditorGUILayout.EnumPopup("Style", _styleOverride);
        }

        _outputName = EditorGUILayout.TextField("Report Name", _outputName);
        EditorGUILayout.Space(8f);
        if (GUILayout.Button("Run Batch Validation", GUILayout.Height(28f)))
        {
            var settings = new BatchRunSettings();
            settings.StartSeed = _startSeed;
            settings.Count = _count;
            settings.MaxAttempts = _maxAttempts;
            settings.UseStyleOverride = _useStyleOverride;
            settings.StyleOverride = _styleOverride;
            settings.OutputName = _outputName;
            _lastSummary = RunBatch(settings).SummaryText;
        }

        if (!string.IsNullOrEmpty(_lastSummary))
        {
            EditorGUILayout.Space(12f);
            EditorGUILayout.HelpBox(_lastSummary, MessageType.Info);
        }

        EditorGUILayout.EndScrollView();
    }

    private static BatchRunResult RunBatch(BatchRunSettings runSettings)
    {
        MvpRuntimeContent.EnsureInitialized();
        var baseSettings = MvpRuntimeContent.Catalog.GeneratorSettings;
        var settings = CloneSettings(baseSettings, runSettings.MaxAttempts);
        var reportRoot = Path.Combine(GetProjectRootPath(), "Reports");
        Directory.CreateDirectory(reportRoot);

        var summary = new BatchValidationSummary();
        summary.GeneratedAt = DateTime.UtcNow.ToString("O");
        summary.StartSeed = runSettings.StartSeed;
        summary.Count = runSettings.Count;
        summary.MaxAttempts = runSettings.MaxAttempts;
        summary.UseStyleOverride = runSettings.UseStyleOverride;
        summary.StyleOverride = runSettings.UseStyleOverride ? runSettings.StyleOverride.ToString() : string.Empty;

        var successCount = 0;
        var totalAttempts = 0f;
        var failCodeFrequency = new Dictionary<ValidationErrorCode, int>();

        for (var i = 0; i < runSettings.Count; i++)
        {
            var seed = runSettings.StartSeed + i;
            var generation = FloorGenerator.Generate(seed, settings, runSettings.UseStyleOverride ? runSettings.StyleOverride : (FloorStyle?)null);
            var validation = FloorGenerator.Validate(generation, settings);
            var detailed = validation.DetailedReport ?? generation.ValidationReport;
            var entry = BuildEntry(seed, generation, detailed);
            summary.Entries.Add(entry);
            totalAttempts += entry.AttemptsUsed;

            if (entry.Success)
            {
                successCount++;
            }
            else if (detailed != null)
            {
                foreach (var error in detailed.Errors)
                {
                    int count;
                    failCodeFrequency.TryGetValue(error.Code, out count);
                    failCodeFrequency[error.Code] = count + 1;
                }
            }
        }

        summary.SuccessRate = runSettings.Count > 0 ? successCount / (float)runSettings.Count : 0f;
        summary.AverageAttempts = runSettings.Count > 0 ? totalAttempts / runSettings.Count : 0f;
        summary.TopFailCodes = failCodeFrequency
            .OrderByDescending(pair => pair.Value)
            .Take(5)
            .Select(pair => string.Format("{0}:{1}", pair.Key, pair.Value))
            .ToList();

        var baseName = string.IsNullOrWhiteSpace(runSettings.OutputName) ? "batch_validation" : runSettings.OutputName.Trim();
        var csvPath = Path.Combine(reportRoot, baseName + ".csv");
        var jsonPath = Path.Combine(reportRoot, baseName + ".json");
        File.WriteAllText(csvPath, BuildCsv(summary));
        File.WriteAllText(jsonPath, JsonUtility.ToJson(summary, true));

        var result = string.Format(
            "Success {0:P0}, avg attempts {1:0.00}, CSV: {2}, JSON: {3}",
            summary.SuccessRate,
            summary.AverageAttempts,
            csvPath,
            jsonPath);

        Debug.Log(string.Format("[GEN] Batch validation finished. {0}", result));
        var runResult = new BatchRunResult();
        runResult.Summary = summary;
        runResult.CsvPath = csvPath;
        runResult.JsonPath = jsonPath;
        runResult.SummaryText = result;
        return runResult;
    }

    private static int ParseIntEnv(string name, int fallback)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        int value;
        return int.TryParse(raw, out value) ? value : fallback;
    }

    private static float ParseFloatEnv(string name, float fallback)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        float value;
        return float.TryParse(raw, out value) ? value : fallback;
    }

    private static string ParseStringEnv(string name, string fallback)
    {
        var raw = Environment.GetEnvironmentVariable(name);
        return string.IsNullOrWhiteSpace(raw) ? fallback : raw.Trim();
    }

    private static string GetProjectRootPath()
    {
        var assetsPath = Application.dataPath;
        if (string.IsNullOrWhiteSpace(assetsPath))
        {
            return Directory.GetCurrentDirectory();
        }

        var projectRoot = Path.GetDirectoryName(assetsPath);
        return string.IsNullOrWhiteSpace(projectRoot) ? Directory.GetCurrentDirectory() : projectRoot;
    }

    private static BatchValidationEntry BuildEntry(int seed, FloorGenerationResult generation, ValidationReport report)
    {
        var entry = new BatchValidationEntry();
        entry.Seed = seed;
        entry.AttemptsUsed = generation != null ? generation.AttemptIndex + 1 : 0;
        entry.Success = report != null && report.Success;
        entry.Style = generation != null ? generation.Style.ToString() : string.Empty;
        if (generation != null && generation.FloorData != null)
        {
            entry.Rooms = generation.FloorData.Rooms.Count;
            entry.Doors = generation.FloorData.Doors.Count;
            entry.Windows = generation.FloorData.Windows.Count;
            entry.Pickups = generation.FloorData.Pickups.Count;
            entry.Props = generation.FloorData.Props.Count;
        }

        entry.FastPathSeconds = ComputeFastPathSeconds(generation, report);
        entry.LootPathSeconds = ComputeLootPathSeconds(generation, report);
        entry.TopErrors = BuildErrorList(report, ValidationSeverity.Error, ValidationSeverity.Fatal);
        entry.Warnings = BuildErrorList(report, ValidationSeverity.Warning);
        return entry;
    }

    private static float ComputeFastPathSeconds(FloorGenerationResult generation, ValidationReport report)
    {
        if (generation == null || generation.FloorData == null || report == null || report.Grid == null || report.DistanceFromSpawn == null)
        {
            return -1f;
        }

        var bestDistance = int.MaxValue;
        for (var i = 0; i < generation.FloorData.Exits.Count; i++)
        {
            var cell = report.Grid.WorldToCell(generation.FloorData.Exits[i].WorldPos);
            if (!report.Grid.InBounds(cell))
            {
                continue;
            }

            var distance = report.DistanceFromSpawn[report.Grid.Index(cell.x, cell.y)];
            if (distance >= 0 && distance < bestDistance)
            {
                bestDistance = distance;
            }
        }

        if (bestDistance == int.MaxValue)
        {
            return -1f;
        }

        return bestDistance * report.CellSize / 4.5f;
    }

    private static float ComputeLootPathSeconds(FloorGenerationResult generation, ValidationReport report)
    {
        if (generation == null || generation.FloorData == null || report == null || report.Grid == null || report.DistanceFromSpawn == null)
        {
            return -1f;
        }

        var farthest = -1;
        for (var i = 0; i < generation.FloorData.Pickups.Count; i++)
        {
            if (!generation.FloorData.Pickups[i].RequiredForMainGate)
            {
                continue;
            }

            var cell = report.Grid.WorldToCell(generation.FloorData.Pickups[i].WorldPos);
            if (!report.Grid.InBounds(cell))
            {
                continue;
            }

            farthest = Mathf.Max(farthest, report.DistanceFromSpawn[report.Grid.Index(cell.x, cell.y)]);
        }

        return farthest >= 0 ? farthest * report.CellSize / 4.5f : -1f;
    }

    private static string BuildErrorList(ValidationReport report, params ValidationSeverity[] severities)
    {
        if (report == null || report.Errors.Count == 0)
        {
            return string.Empty;
        }

        var accepted = new HashSet<ValidationSeverity>(severities);
        var frequencies = new Dictionary<ValidationErrorCode, int>();
        for (var i = 0; i < report.Errors.Count; i++)
        {
            var error = report.Errors[i];
            if (!accepted.Contains(error.Severity))
            {
                continue;
            }

            int count;
            frequencies.TryGetValue(error.Code, out count);
            frequencies[error.Code] = count + 1;
        }

        return string.Join(";", frequencies.OrderByDescending(pair => pair.Value).Select(pair => string.Format("{0}:{1}", pair.Key, pair.Value)).Take(4).ToArray());
    }

    private static string BuildCsv(BatchValidationSummary summary)
    {
        var writer = new StringWriter();
        writer.WriteLine("Seed,AttemptsUsed,Success,Style,Rooms,Doors,Windows,Pickups,Props,FastPathSeconds,LootPathSeconds,TopErrors,Warnings");
        for (var i = 0; i < summary.Entries.Count; i++)
        {
            var entry = summary.Entries[i];
            writer.WriteLine(string.Format(
                "{0},{1},{2},{3},{4},{5},{6},{7},{8},{9:0.00},{10:0.00},\"{11}\",\"{12}\"",
                entry.Seed,
                entry.AttemptsUsed,
                entry.Success ? 1 : 0,
                entry.Style,
                entry.Rooms,
                entry.Doors,
                entry.Windows,
                entry.Pickups,
                entry.Props,
                entry.FastPathSeconds,
                entry.LootPathSeconds,
                entry.TopErrors,
                entry.Warnings));
        }

        return writer.ToString();
    }

    private static FloorGeneratorSettings CloneSettings(FloorGeneratorSettings source, int maxAttempts)
    {
        var clone = ScriptableObject.CreateInstance<FloorGeneratorSettings>();
        clone.hideFlags = HideFlags.HideAndDontSave;
        var validation = CloneValidationConfig(source.ValidationConfig, maxAttempts);
        clone.Configure(
            source.CellSize,
            source.FloorThickness,
            source.WallHeight,
            source.WallThickness,
            source.ExtraRoomsMin,
            source.ExtraRoomsMax,
            source.ValidationRuns,
            source.Templates,
            validation,
            source.OutsideThemeProfile,
            source.OfficeRules);
        return clone;
    }

    private static ValidationConfig CloneValidationConfig(ValidationConfig source, int maxAttempts)
    {
        var clone = new ValidationConfig();
        if (source != null)
        {
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), clone);
        }

        SetPrivateField(clone, "_maxValidationAttempts", maxAttempts);
        return clone;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}
}

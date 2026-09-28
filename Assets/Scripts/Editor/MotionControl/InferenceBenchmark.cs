using System;
using System.Diagnostics;
using System.Text;
using Unity.InferenceEngine;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class InferenceBenchmark
{
    private const int WARMUP_RUNS = 30;
    private const int MEASURED_RUNS = 20;

    private static readonly BackendType[] Backends = { BackendType.GPUCompute, BackendType.CPU, BackendType.GPUPixel };

    [MenuItem("Shipyard Surfers/Motion Control/Benchmark Inference Backends")]
    public static void Run()
    {
        MotionControlConfig config = MotionControlConfig.LoadDefault();
        if (config == null) throw new InvalidOperationException($"missing config at Resources/{MotionControlConfig.RESOURCE_PATH}");

        Model model = ModelLoader.Load(config.Model);
        var report = new StringBuilder();
        report.AppendLine($"{nameof(InferenceBenchmark)}: {model.layers.Count} layers, input {config.InputSize}, configured backend {config.Backend}, " +
                          $"graphics '{SystemInfo.graphicsDeviceName}' ({SystemInfo.graphicsDeviceType}, compute {SystemInfo.supportsComputeShaders}), " +
                          $"cpu '{SystemInfo.processorType}' x{SystemInfo.processorCount}");
        foreach (BackendType backend in Backends) report.AppendLine(Measure(model, backend, config.InputSize));
        Debug.Log(report.ToString());
    }

    public static void RunBatch()
    {
        try
        {
            Run();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            EditorApplication.Exit(1);
        }
    }

    private static string Measure(Model model, BackendType backend, int inputSize)
    {
        try
        {
            using var worker = new Worker(model, backend);
            using var input = new Tensor<float>(new TensorShape(1, 3, inputSize, inputSize));
            var samples = new float[MEASURED_RUNS];
            var stopwatch = new Stopwatch();

            for (int run = 0; run < WARMUP_RUNS + MEASURED_RUNS; run++)
            {
                stopwatch.Restart();
                worker.Schedule(input);
                using Tensor output = worker.PeekOutput().ReadbackAndClone();
                stopwatch.Stop();
                if (run >= WARMUP_RUNS) samples[run - WARMUP_RUNS] = (float)stopwatch.Elapsed.TotalMilliseconds;
            }

            Array.Sort(samples);
            return $"  {backend}: median {samples[MEASURED_RUNS / 2]:0.0} ms, min {samples[0]:0.0} ms, max {samples[MEASURED_RUNS - 1]:0.0} ms";
        }
        catch (Exception exception)
        {
            return $"  {backend}: FAILED ({exception.Message})";
        }
    }
}

using System;
using System.Collections;
using Unity.InferenceEngine;
using Unity.Jobs;
using UnityEngine;

public class PoseModelRunner : IDisposable
{
    private readonly Worker _worker;
    private readonly Tensor<float> _input;
    private readonly TextureTransform _transform;
    private readonly DispatchPlanner _planner;
    private readonly bool _cpuBackend;

    private IEnumerator _schedule;
    private Tensor<float> _pendingOutput;
    private bool _awaitingInput;
    private float _startedAt;
    private int _dispatchFrames;

    public int InputSize { get; }
    public int LayerCount { get; }
    public BackendType Backend { get; }
    public float LastInferenceMs { get; private set; }
    public int LastDispatchFrames { get; private set; }
    public float StartedAt => _startedAt;
    public bool IsIdle => !_awaitingInput && _schedule == null && _pendingOutput == null;

    public PoseModelRunner(Model model, int inputSize, BackendType backend, DispatchPlanner planner)
    {
        InputSize = inputSize;
        Backend = backend;
        LayerCount = model.layers.Count;
        _planner = planner;
        _cpuBackend = backend == BackendType.CPU;

        _worker = new Worker(model, backend);
        _input = new Tensor<float>(new TensorShape(1, 3, inputSize, inputSize));
        _transform = new TextureTransform().SetCoordOrigin(CoordOrigin.TopLeft);
    }

    public void Begin(Texture input, float now)
    {
        if (!IsIdle) throw new InvalidOperationException("Previous inference has not been consumed yet.");

        TextureConverter.ToTensor(input, _input, _transform);
        _startedAt = now;
        _dispatchFrames = 0;

        if (_cpuBackend)
        {
            _input.ReadbackRequest();
            _awaitingInput = true;
            return;
        }

        _schedule = _worker.ScheduleIterable(_input);
    }

    public void Abort()
    {
        _schedule = null;
        _pendingOutput = null;
        _awaitingInput = false;
    }

    public void Step()
    {
        if (_awaitingInput)
        {
            if (!_input.IsReadbackRequestDone()) return;

            _awaitingInput = false;
            _schedule = _worker.ScheduleIterable(_input);
        }

        if (_schedule == null) return;

        _dispatchFrames++;
        int layers = _cpuBackend ? int.MaxValue : _planner.LayersPerFrame(LayerCount);
        for (int i = 0; i < layers; i++)
        {
            if (_schedule.MoveNext()) continue;

            FinishDispatch();
            return;
        }
    }

    public bool TryTakeResult(float now, out float[] output, out int anchorCount)
    {
        output = null;
        anchorCount = 0;

        if (_pendingOutput == null) return false;
        if (!_pendingOutput.IsReadbackRequestDone()) return false;

        output = _pendingOutput.DownloadToArray();
        anchorCount = _pendingOutput.shape[2];
        _pendingOutput = null;
        LastInferenceMs = (now - _startedAt) * 1000f;
        return true;
    }

    public void Dispose()
    {
        Abort();
        _input?.Dispose();
        _worker?.Dispose();
    }

    private void FinishDispatch()
    {
        _schedule = null;
        LastDispatchFrames = _dispatchFrames;
        _pendingOutput = _worker.PeekOutput() as Tensor<float>;
        if (_cpuBackend) JobHandle.ScheduleBatchedJobs();
        _pendingOutput?.ReadbackRequest();
    }
}

using System;
using System.Collections;
using Unity.InferenceEngine;
using UnityEngine;

public class PoseModelRunner : IDisposable
{
    private readonly Worker _worker;
    private readonly Tensor<float> _input;
    private readonly TextureTransform _transform;
    private readonly int _layersPerFrame;

    private IEnumerator _schedule;
    private Tensor<float> _pendingOutput;
    private float _startedAt;

    public int InputSize { get; }
    public float LastInferenceMs { get; private set; }
    public bool IsIdle => _schedule == null && _pendingOutput == null;

    public PoseModelRunner(Model model, int inputSize, int layersPerFrame, BackendType backend)
    {
        InputSize = inputSize;
        _layersPerFrame = Mathf.Max(1, layersPerFrame);

        _worker = new Worker(model, backend);
        _input = new Tensor<float>(new TensorShape(1, 3, inputSize, inputSize));
        _transform = new TextureTransform().SetCoordOrigin(CoordOrigin.TopLeft);
    }

    public void Begin(Texture input, float now)
    {
        if (!IsIdle) throw new InvalidOperationException("Previous inference has not been consumed yet.");

        TextureConverter.ToTensor(input, _input, _transform);
        _schedule = _worker.ScheduleIterable(_input);
        _startedAt = now;
    }

    public float StartedAt => _startedAt;

    public void Abort()
    {
        _schedule = null;
        _pendingOutput = null;
    }

    public void Step()
    {
        if (_schedule == null) return;

        for (int i = 0; i < _layersPerFrame; i++)
        {
            if (_schedule.MoveNext()) continue;

            _schedule = null;
            _pendingOutput = _worker.PeekOutput() as Tensor<float>;
            _pendingOutput?.ReadbackRequest();
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
        _schedule = null;
        _pendingOutput = null;
        _input?.Dispose();
        _worker?.Dispose();
    }
}

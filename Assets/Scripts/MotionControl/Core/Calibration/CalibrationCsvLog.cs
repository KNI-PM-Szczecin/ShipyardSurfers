using System.Globalization;
using System.Text;

public sealed class CalibrationCsvLog
{
    private const string SEPARATOR = ",";
    private const string NUMBER_FORMAT = "0.#####";

    private readonly StringBuilder _builder = new StringBuilder();

    public CalibrationCsvLog()
    {
        AppendHeader();
    }

    public int SampleCount { get; private set; }

    public void AppendSample(int round, int stepInRound, GestureType prompt, CalibrationPhase phase, int sample, float elapsed,
        TrackingState state, GestureType? fired, NormalizedPose pose)
    {
        _builder.Append(round).Append(SEPARATOR)
            .Append(stepInRound).Append(SEPARATOR)
            .Append(prompt).Append(SEPARATOR)
            .Append(phase).Append(SEPARATOR)
            .Append(sample).Append(SEPARATOR)
            .Append(Number(pose.Timestamp)).Append(SEPARATOR)
            .Append(Number(elapsed)).Append(SEPARATOR)
            .Append(state).Append(SEPARATOR)
            .Append(fired.HasValue ? fired.Value.ToString() : string.Empty).Append(SEPARATOR)
            .Append(Number(pose.ShoulderCenter.x)).Append(SEPARATOR)
            .Append(Number(pose.ShoulderCenter.y)).Append(SEPARATOR)
            .Append(Number(pose.ShoulderWidth)).Append(SEPARATOR)
            .Append(Number(pose.HipLine));

        for (int i = 0; i < KeypointIndex.COUNT; i++)
        {
            _builder.Append(SEPARATOR).Append(Number(pose.Points[i].x))
                .Append(SEPARATOR).Append(Number(pose.Points[i].y))
                .Append(SEPARATOR).Append(Number(pose.Confidences[i]));
        }

        _builder.Append('\n');
        SampleCount++;
    }

    public string ToCsv() => _builder.ToString();

    private void AppendHeader()
    {
        _builder.Append("round,step,prompt,phase,sample,time,elapsed,state,fired,")
            .Append("shoulder_center_x,shoulder_center_y,shoulder_width,hip_line");

        foreach (string name in KeypointIndex.Names)
        {
            _builder.Append(SEPARATOR).Append(name).Append("_x")
                .Append(SEPARATOR).Append(name).Append("_y")
                .Append(SEPARATOR).Append(name).Append("_c");
        }

        _builder.Append('\n');
    }

    private static string Number(float value) => value.ToString(NUMBER_FORMAT, CultureInfo.InvariantCulture);
}

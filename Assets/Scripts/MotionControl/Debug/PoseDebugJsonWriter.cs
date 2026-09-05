using System.Globalization;
using System.Text;
using UnityEngine;

public class PoseDebugJsonWriter
{
    private const string FLOAT_FORMAT = "0.####";

    private readonly StringBuilder _builder = new StringBuilder(2048);

    public string Write(PoseDebugSnapshot snapshot)
    {
        _builder.Clear();
        _builder.Append('{');

        AppendString("state", snapshot.State.ToString());
        AppendBool("hasPerson", snapshot.HasPerson);
        AppendFloat("personConfidence", snapshot.PersonConfidence);
        AppendBool("poseValid", snapshot.PoseValid);
        AppendBool("armedVertical", snapshot.ArmedVertical);
        AppendBool("armedHorizontal", snapshot.ArmedHorizontal);
        AppendBool("mirror", snapshot.Mirror);
        AppendFloat("poseFps", snapshot.PoseFps);
        AppendFloat("inferenceMs", snapshot.InferenceMs);
        AppendFloat("now", snapshot.Now);
        AppendString("lastGesture", snapshot.LastGesture);
        AppendFloat("lastGestureTime", snapshot.LastGestureTime);

        _builder.Append("\"bbox\":[");
        AppendNumber(snapshot.BoundingBox.x); _builder.Append(',');
        AppendNumber(snapshot.BoundingBox.y); _builder.Append(',');
        AppendNumber(snapshot.BoundingBox.width); _builder.Append(',');
        AppendNumber(snapshot.BoundingBox.height);
        _builder.Append("],");

        _builder.Append("\"kpts\":[");
        for (int i = 0; i < KeypointIndex.COUNT; i++)
        {
            if (i > 0) _builder.Append(',');
            Vector2 point = snapshot.Keypoints[i];
            AppendNumber(point.x); _builder.Append(',');
            AppendNumber(point.y); _builder.Append(',');
            AppendNumber(snapshot.Confidences[i]);
        }
        _builder.Append("]}");

        return _builder.ToString();
    }

    private void AppendString(string key, string value)
    {
        _builder.Append('"').Append(key).Append("\":\"").Append(value).Append("\",");
    }

    private void AppendBool(string key, bool value)
    {
        _builder.Append('"').Append(key).Append("\":").Append(value ? "true" : "false").Append(',');
    }

    private void AppendFloat(string key, float value)
    {
        _builder.Append('"').Append(key).Append("\":");
        AppendNumber(value);
        _builder.Append(',');
    }

    private void AppendNumber(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value)) value = 0f;
        _builder.Append(value.ToString(FLOAT_FORMAT, CultureInfo.InvariantCulture));
    }
}

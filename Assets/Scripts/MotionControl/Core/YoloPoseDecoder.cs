using UnityEngine;

public class YoloPoseDecoder
{
    public const int VALUES_PER_ANCHOR = 5 + KeypointIndex.COUNT * 3;

    private const int CENTER_X_ROW = 0;
    private const int CENTER_Y_ROW = 1;
    private const int WIDTH_ROW = 2;
    private const int HEIGHT_ROW = 3;
    private const int CONFIDENCE_ROW = 4;
    private const int FIRST_KEYPOINT_ROW = 5;

    private readonly float _minConfidence;

    public YoloPoseDecoder(float minConfidence)
    {
        _minConfidence = minConfidence;
    }

    public bool TryDecode(float[] output, int anchorCount, int inputWidth, int inputHeight, float timestamp, PoseFrame target)
    {
        target.Clear(timestamp, inputWidth, inputHeight);

        if (output == null || anchorCount <= 0 || output.Length < VALUES_PER_ANCHOR * anchorCount)
        {
            return false;
        }

        int best = FindBestAnchor(output, anchorCount);
        if (best < 0)
        {
            return false;
        }

        float centerX = output[CENTER_X_ROW * anchorCount + best];
        float centerY = output[CENTER_Y_ROW * anchorCount + best];
        float width = output[WIDTH_ROW * anchorCount + best];
        float height = output[HEIGHT_ROW * anchorCount + best];

        target.HasPerson = true;
        target.Confidence = output[CONFIDENCE_ROW * anchorCount + best];
        target.BoundingBox = new Rect(centerX - width * 0.5f, centerY - height * 0.5f, width, height);

        for (int k = 0; k < KeypointIndex.COUNT; k++)
        {
            int row = FIRST_KEYPOINT_ROW + k * 3;
            float x = output[row * anchorCount + best];
            float y = output[(row + 1) * anchorCount + best];
            float visibility = output[(row + 2) * anchorCount + best];
            target.Keypoints[k] = new Keypoint(new Vector2(x, y), visibility);
        }

        return true;
    }

    private int FindBestAnchor(float[] output, int anchorCount)
    {
        int bestIndex = -1;
        float bestConfidence = _minConfidence;
        int offset = CONFIDENCE_ROW * anchorCount;

        for (int a = 0; a < anchorCount; a++)
        {
            float confidence = output[offset + a];
            if (confidence > bestConfidence)
            {
                bestConfidence = confidence;
                bestIndex = a;
            }
        }

        return bestIndex;
    }
}

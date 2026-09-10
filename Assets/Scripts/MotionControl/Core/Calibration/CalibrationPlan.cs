using System;
using System.Collections.Generic;

public sealed class CalibrationPlan
{
    public static readonly GestureType[] Gestures =
    {
        GestureType.LaneLeft, GestureType.LaneRight, GestureType.Jump, GestureType.Roll
    };

    private readonly List<GestureType> _steps = new List<GestureType>();

    public CalibrationPlan(int rounds, Random random)
    {
        var round = new List<GestureType>(Gestures);

        for (int i = 0; i < Math.Max(1, rounds); i++)
        {
            Shuffle(round, random);
            _steps.AddRange(round);
        }
    }

    public int Count => _steps.Count;

    public int StepsPerRound => Gestures.Length;

    public GestureType Gesture(int index) => _steps[Math.Min(Math.Max(index, 0), _steps.Count - 1)];

    public int Round(int index) => index / StepsPerRound + 1;

    public int StepInRound(int index) => index % StepsPerRound + 1;

    private static void Shuffle(List<GestureType> items, Random random)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            GestureType swapped = items[i];
            items[i] = items[j];
            items[j] = swapped;
        }
    }
}

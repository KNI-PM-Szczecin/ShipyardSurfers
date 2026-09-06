using System.Runtime.InteropServices;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.InputSystem.Utilities;

[StructLayout(LayoutKind.Explicit, Size = 4)]
public struct PoseGestureState : IInputStateTypeInfo
{
    public const int UP_BIT = 0;
    public const int DOWN_BIT = 1;
    public const int LEFT_BIT = 2;
    public const int RIGHT_BIT = 3;

    public static FourCC Format => new FourCC('P', 'O', 'S', 'E');

    public FourCC format => Format;

    [InputControl(name = "up", layout = "Button", bit = UP_BIT, displayName = "Hands Up")]
    [InputControl(name = "down", layout = "Button", bit = DOWN_BIT, displayName = "Hands Down")]
    [InputControl(name = "left", layout = "Button", bit = LEFT_BIT, displayName = "Swipe Left")]
    [InputControl(name = "right", layout = "Button", bit = RIGHT_BIT, displayName = "Swipe Right")]
    [FieldOffset(0)]
    public byte buttons;

    public static PoseGestureState Pressed(int bit)
    {
        return new PoseGestureState { buttons = (byte)(1 << bit) };
    }

    public static PoseGestureState Released => default;
}

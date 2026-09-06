using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.Layouts;
#if UNITY_EDITOR
using UnityEditor;
#endif

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
[InputControlLayout(displayName = "Pose Gesture Device", stateType = typeof(PoseGestureState))]
public class PoseGestureDevice : InputDevice
{
    public ButtonControl Up { get; private set; }
    public ButtonControl Down { get; private set; }
    public ButtonControl Left { get; private set; }
    public ButtonControl Right { get; private set; }

    static PoseGestureDevice()
    {
        InputSystem.RegisterLayout<PoseGestureDevice>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureLayoutRegistered()
    {
    }

    protected override void FinishSetup()
    {
        base.FinishSetup();
        Up = GetChildControl<ButtonControl>("up");
        Down = GetChildControl<ButtonControl>("down");
        Left = GetChildControl<ButtonControl>("left");
        Right = GetChildControl<ButtonControl>("right");
    }
}

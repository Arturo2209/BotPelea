using UnityEngine;

/// <summary>Teclado y mouse: funciona con el Input System nuevo o con el Input Manager clásico.</summary>
public static class Entrada
{
#if ENABLE_INPUT_SYSTEM
    static UnityEngine.InputSystem.Keyboard K => UnityEngine.InputSystem.Keyboard.current;
    static UnityEngine.InputSystem.Mouse M => UnityEngine.InputSystem.Mouse.current;

    public static Vector2 Ejes()
    {
        if (K == null) return Vector2.zero;
        float x = (K.dKey.isPressed || K.rightArrowKey.isPressed ? 1 : 0) - (K.aKey.isPressed || K.leftArrowKey.isPressed ? 1 : 0);
        float y = (K.wKey.isPressed || K.upArrowKey.isPressed ? 1 : 0) - (K.sKey.isPressed || K.downArrowKey.isPressed ? 1 : 0);
        return new Vector2(x, y);
    }
    public static bool Correr() => K != null && K.leftShiftKey.isPressed;
    public static bool Saltar() => K != null && K.spaceKey.wasPressedThisFrame;
    public static Vector2 Mouse() => M != null ? M.delta.ReadValue() * 0.1f : Vector2.zero;
    public static float Rueda() => M != null ? M.scroll.ReadValue().y / 120f : 0f;
    public static bool Reiniciar() => K != null && K.rKey.wasPressedThisFrame;
    public static bool CambiarModo() => K != null && K.eKey.wasPressedThisFrame;
    public static bool CambiarCamara() => K != null && K.cKey.wasPressedThisFrame;
    public static bool GolpeRapido() => (M != null && M.leftButton.wasPressedThisFrame) || (K != null && K.jKey.wasPressedThisFrame);
    public static bool GolpeFuerte() => (M != null && M.rightButton.wasPressedThisFrame) || (K != null && K.kKey.wasPressedThisFrame);
    public static bool Bloquear() => K != null && (K.fKey.isPressed || K.lKey.isPressed);
#else
    public static Vector2 Ejes() => new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
    public static bool Correr() => Input.GetKey(KeyCode.LeftShift);
    public static bool Saltar() => Input.GetKeyDown(KeyCode.Space);
    public static Vector2 Mouse() => new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
    public static float Rueda() => Input.mouseScrollDelta.y;
    public static bool Reiniciar() => Input.GetKeyDown(KeyCode.R);
    public static bool CambiarModo() => Input.GetKeyDown(KeyCode.E);
    public static bool CambiarCamara() => Input.GetKeyDown(KeyCode.C);
    public static bool GolpeRapido() => Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.J);
    public static bool GolpeFuerte() => Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.K);
    public static bool Bloquear() => Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.L);
#endif
}

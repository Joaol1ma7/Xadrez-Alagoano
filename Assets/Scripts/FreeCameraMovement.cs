using UnityEngine;

[DisallowMultipleComponent]
public class FreeCameraMovement : MonoBehaviour
{
    [SerializeField, Min(0f)] private float moveSpeed = 4f;
    [SerializeField, Min(0f)] private float mouseSensitivity = 2f;

    private float yaw;
    private float pitch;
    private bool looking;
    private CursorLockMode previousCursorLock;
    private bool previousCursorVisible;

    void Update()
    {
        // 0 btn esquerdo, 1 bn direito, 2 scroll 
        if (Input.GetMouseButtonDown(1)) 
            BeginLook();

        if (Input.GetMouseButtonUp(1) || Input.GetKeyDown(KeyCode.Escape)) EndLook();

        if (looking)
        {
            if (Cursor.lockState == CursorLockMode.Locked)
                Look(new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")));
            else
                EndLook();
        }

        float horizontal = (Input.GetKey(KeyCode.D) ? 1f : 0f) -
                           (Input.GetKey(KeyCode.A) ? 1f : 0f);
        float forward = (Input.GetKey(KeyCode.W) ? 1f : 0f) -
                        (Input.GetKey(KeyCode.S) ? 1f : 0f);
        float vertical = (Input.GetKey(KeyCode.Space) ? 1f : 0f) -
                         (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ? 1f : 0f);

        bool boost = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        Move(new Vector3(horizontal, vertical, forward), Time.deltaTime, boost);
    }

    void BeginLook()
    {
        if (looking) return;
        yaw = transform.eulerAngles.y;
        pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, transform.eulerAngles.x), -89f, 89f);
        previousCursorLock = Cursor.lockState;
        previousCursorVisible = Cursor.visible;
        looking = true;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void EndLook()
    {
        if (!looking) return;
        looking = false;
        Cursor.lockState = previousCursorLock;
        Cursor.visible = previousCursorVisible;
    }

    void Look(Vector2 mouseDelta)
    {
        // Mouse X/Y já representam deslocamento por frame: não multiplicar por deltaTime.
        yaw = Mathf.Repeat(yaw + mouseDelta.x * mouseSensitivity, 360f);
        pitch = Mathf.Clamp(pitch - mouseDelta.y * mouseSensitivity, -89f, 89f);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    void OnDisable() => EndLook();

    void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus) EndLook();
    }

    void Move(Vector3 input, float deltaTime, bool boost)
    {
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up);
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up);

        // Na vista vertical inicial, forward não tem projeção horizontal.
        // O eixo lateral preserva a orientação do tabuleiro nesse caso.
        if (forward.sqrMagnitude < 0.000001f)
            forward = Vector3.Cross(right, Vector3.up);
        if (right.sqrMagnitude < 0.000001f)
            right = Vector3.Cross(Vector3.up, forward);

        Vector3 direction = forward.normalized * input.z +
                            right.normalized * input.x + Vector3.up * input.y;
        float currentSpeed = moveSpeed * (boost ? 1.5f : 1f);
        transform.position += Vector3.ClampMagnitude(direction, 1f) * currentSpeed * deltaTime;
    }
}

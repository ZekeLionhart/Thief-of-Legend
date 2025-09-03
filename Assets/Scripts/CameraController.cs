using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private PlayerController player;
    [SerializeField] private float xOffset;
    [SerializeField] private float yOffset;
    [SerializeField] private float smoothTime;

    private float xMult;
    private Vector3 velocity = Vector3.zero;
    private Vector3 offset = new Vector3(0f, 0f, -10f);
    private Vector3 playerPosition = new Vector3();

    void FixedUpdate()
    {
        if (player != null)
        {
            PositionCamera();
        }
    }

    private void PositionCamera()
    {
        SetupCameraOffset();

        playerPosition = player.transform.position + offset;

        transform.position = Vector3.SmoothDamp(transform.position, playerPosition, ref velocity, smoothTime);
    }

    private void SetupCameraOffset()
    {
        CheckPlayerDirection();

        offset.x = xOffset * xMult;
        offset.y = yOffset;
    }

    private void CheckPlayerDirection()
    {
        float verticalSpeed = player.GetComponent<Rigidbody2D>().velocity.y;

        if (player.move > 0)
            xMult = 1f;
        else if (player.move < 0)
            xMult = -1f;
    }
}

using UnityEngine;

public class EnemySight : MonoBehaviour
{
    [SerializeField] private Transform eyesOrigin;
    [SerializeField] private Transform sightDirection;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float sightDistance;
    [SerializeField] private float fovAngle;
    [SerializeField] private int fovResolution;
    [SerializeField] private FieldOfView fieldOfView;
    private Transform playerHead;
    private Transform playerFeet;
    private bool isPursuing = false;
    private Vector2 origin;
    private Vector2 targetDirection;
    private Vector2 fovDirection;

    private void Awake()
    {
        if (playerHead == null) playerHead = GameObject.Find("Head").transform;
        if (playerFeet == null) playerFeet = GameObject.Find("Feet").transform;
        origin = eyesOrigin.position;
        fovDirection = ((Vector2)sightDirection.position - origin).normalized;
    }

    private void Update()
    {
        origin = eyesOrigin.position;
        fovDirection = ((Vector2)sightDirection.position - origin).normalized;
    }

    private void LateUpdate()
    {
        DrawFOV();
    }

    public bool CallSightCheck()
    {
        return CheckLineOfSight(playerHead) || CheckLineOfSight(playerFeet);
    }

    private bool CheckLineOfSight(Transform target)
    {
        targetDirection = ((Vector2)target.position - origin).normalized;
        RaycastHit2D hit = Physics2D.Raycast(origin, targetDirection, sightDistance, collisionMask);

        if (hit.collider == null)
        {
            Debug.DrawLine(origin, origin + targetDirection * sightDistance, Color.green);
            return false;
        }

        if (hit.collider.gameObject.layer == LayerMask.NameToLayer("PlayerMarker")
            && Vector2.Angle(fovDirection, targetDirection) < fovAngle / 2)
        {
            Debug.DrawLine(origin, hit.point, Color.red);
            return true;
        }

        Debug.DrawLine(origin, hit.point, Color.green);
        return false;
    }

    private void DrawFOV()
    {
        fieldOfView.SetOrigin(origin);
        if (isPursuing) fieldOfView.SetAimDirection(((Vector2)playerHead.transform.position - origin).normalized);
        else fieldOfView.SetAimDirection(fovDirection);
        fieldOfView.FovAngle = fovAngle;
        fieldOfView.SightDistance = sightDistance;
        fieldOfView.RayCount = fovResolution;
    }

    public void SetPursuit(bool mode)
    {
        isPursuing = mode;
    }
}
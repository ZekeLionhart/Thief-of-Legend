using UnityEngine;

public class EnemySight : MonoBehaviour
{
    //public bool PlayerInSight { get; private set; }

    [SerializeField] private Transform eyesOrigin;
    [SerializeField] private LayerMask collisionMask;
    [SerializeField] private float sightDistance;
    [SerializeField] private float fovAngle;
    private Transform playerHead;
    private Transform playerFeet;
    private int directionMod = 1;

    private void Awake()
    {
        if (playerHead == null) playerHead = GameObject.Find("Head").transform;
        if (playerFeet == null) playerFeet = GameObject.Find("Feet").transform;
    }

    public bool CallSightCheck()
    {
        return CheckLineOfSight(playerHead) || CheckLineOfSight(playerFeet);
    }

    private bool CheckLineOfSight(Transform target)
    {
        Vector2 origin = eyesOrigin.position;
        Vector2 targetDirection = (target.position - eyesOrigin.position).normalized;
        Vector2 sightDirection = new Vector2(sightDistance, 0f) * directionMod;
        Vector2 sight = origin + sightDirection;

        DrawFOV(origin, sight, sightDirection);
        
        // Cast the ray
        RaycastHit2D hit = Physics2D.Raycast(origin, targetDirection, sightDistance, collisionMask);

        if (hit.collider == null)
        {
            Debug.DrawLine(origin, origin + targetDirection * sightDistance, Color.green);
            //PlayerInSight = false;
            return false;
        }

        if (hit.collider.gameObject.layer == LayerMask.NameToLayer("PlayerMarker") 
                 && Vector2.Angle((sight - origin).normalized, targetDirection) < fovAngle/2)
        {
            Debug.DrawLine(origin, hit.point, Color.red);
            //PlayerInSight = true;
            return true;
        }

        Debug.DrawLine(origin, hit.point, Color.green);
        //PlayerInSight = false;
        return false;
    }

    private void DrawFOV(Vector2 origin, Vector2 sight, Vector2 sightDirection)
    {
        Vector2 rotatedDirectionUp = Quaternion.Euler(0, 0, -fovAngle / 2) * sightDirection;
        Vector2 endUp = origin + rotatedDirectionUp;
        Debug.DrawLine(origin, endUp, Color.yellow);

        Vector2 rotatedDirectionDown = Quaternion.Euler(0, 0, fovAngle / 2) * sightDirection;
        Vector2 endDown = origin + rotatedDirectionDown;
        Debug.DrawLine(origin, endDown, Color.yellow);

        Debug.DrawLine(origin, sight, Color.yellow);
    }

    public void SetDirection(Enemy.Direction direction)
    {
        if (direction == Enemy.Direction.Left) { directionMod = -1; return; }
        if (direction == Enemy.Direction.Right) { directionMod = 1; return; }
    }
}

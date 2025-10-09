using UnityEngine;

public class FieldOfView : MonoBehaviour
{
    [SerializeField] private LayerMask m_LayerMask;
    public float FovAngle { set; get; }
    public float SightDistance { set; get; }
    public int RayCount { set; get; }
    private Vector3 origin = Vector3.zero;
    private float startingAimDirection;
    private Mesh mesh;

    private void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        origin = Vector3.zero;
    }

    private void LateUpdate()
    {
        float fovDirection = startingAimDirection;
        float angleIncrease = FovAngle / RayCount;

        Vector3[] vertices = new Vector3[RayCount + 1 + 1];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[RayCount * 3];

        vertices[0] = origin;

        int vertexIndex = 1;
        int triangleIndex = 0;
        for (int i = 0; i <= RayCount; i++)
        {
            float angleRad = fovDirection * (Mathf.PI / 180f);
            Vector3 angleCosSin = new Vector3(Mathf.Cos(angleRad), Mathf.Sin(angleRad));
            Vector3 vertex;
            RaycastHit2D raycastHit2D = Physics2D.Raycast(origin, angleCosSin, SightDistance, m_LayerMask);

            if (raycastHit2D.collider == null)
                vertex = origin + angleCosSin * SightDistance;
            else vertex = raycastHit2D.point;

            vertices[vertexIndex] = vertex;

            if (i > 0)
            {
                triangles[triangleIndex + 0] = 0;
                triangles[triangleIndex + 1] = vertexIndex - 1;
                triangles[triangleIndex + 2] = vertexIndex;

                triangleIndex += 3;
            }

            vertexIndex++;
            fovDirection -= angleIncrease;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.bounds = new Bounds(origin, Vector2.one * 100f);
    }

    public void SetOrigin(Vector3 origin)
    {
        this.origin = origin;
    }

    public void SetAimDirection(Vector3 aimDirection)
    {
        aimDirection = aimDirection.normalized;
        float angle = Mathf.Atan2(aimDirection.y, aimDirection.x) * Mathf.Rad2Deg;

        if (angle < 0) angle += 360;

        startingAimDirection = angle + FovAngle / 2f;
    }
}
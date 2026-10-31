using UnityEngine;

public class JumpPad : MonoBehaviour
{
    [SerializeField] private Transform landingPad;

    public Transform GetLandingPad => landingPad;
}

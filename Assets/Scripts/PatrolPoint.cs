using UnityEngine;

public class PatrolPoint : MonoBehaviour
{
    [field: SerializeField] public float WaitTime { get; private set; }
    [field: SerializeField] public bool TurnAround { get; private set; }
}
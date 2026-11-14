using UnityEngine;

public class SlowRotate : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 2f; // negative = rotate the other way

    private void Update()
    {
        transform.Rotate(0f, 0f, degreesPerSecond * Time.deltaTime);
    }
}
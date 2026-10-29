using UnityEngine;

public class ClockShake : MonoBehaviour
{
    [Header("Normal Shake")]
    [SerializeField] private float normalShakeAngle = 8f;
    [SerializeField] private float normalShakeSpeed = 20f;

    [Header("Final 5 Seconds")]
    [SerializeField] private float intenseShakeAngle = 14f;
    [SerializeField] private float intenseShakeSpeed = 40f;

    private Quaternion initialRotation;
    private bool isShaking;
    private bool isIntense;

    private void Awake()
    {
        initialRotation = transform.localRotation;
    }

    private void Update()
    {
        if (!isShaking)
            return;

        float angle = isIntense ? intenseShakeAngle : normalShakeAngle;
        float speed = isIntense ? intenseShakeSpeed : normalShakeSpeed;

        float shake = Mathf.Sin(Time.time * speed) * angle;

        transform.localRotation =
            initialRotation * Quaternion.Euler(0f, 0f, shake);
    }

    public void StartShaking()
    {
        isShaking = true;
        isIntense = false;
    }

    public void StartIntenseShaking()
    {
        isIntense = true;
    }

    public void StopShaking()
    {
        isShaking = false;
        isIntense = false;
        transform.localRotation = initialRotation;
    }
}
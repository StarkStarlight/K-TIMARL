using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipStabilizer : MonoBehaviour
{
    [Header("Stabilization Settings")]
    [SerializeField] private float uprightForce = 10f;
    [SerializeField] private float uprightTorque = 5f;
    [SerializeField] private float stabilityThreshold = 0.6f; 
    [SerializeField] private float submergedFactor = 0.8f;

    [Header("Buoyancy Settings")]
    [SerializeField] private Transform[] buoyancyPoints;
    [SerializeField] private float buoyancyForce = 5f;
    [SerializeField] private float waterLevel = 0f;

    [SerializeField] private float capsizedThreshold = 0.3f;
    [SerializeField] private float recoveryDelay = 3f;
    [SerializeField] private float emergencyTorque = 20f;

    private Rigidbody rb;
    private float originalDrag;
    private float originalAngularDrag;

    private float capsizedTimer = 0f;
    private bool isCapsized = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        originalDrag = rb.drag;
        originalAngularDrag = rb.angularDrag;
    }

    void FixedUpdate()
    {
        ApplyBuoyancy();
        StabilizeShip();
        AdjustDrag();
        CheckCapsized(); 
    }

    void ApplyBuoyancy()
    {
        foreach (Transform point in buoyancyPoints)
        {
            float depth = waterLevel - point.position.y;

            if (depth > 0)
            {
                float buoyancy = Mathf.Clamp01(depth) * buoyancyForce;
                rb.AddForceAtPosition(Vector3.up * buoyancy, point.position, ForceMode.Force);
            }
        }
    }

    void StabilizeShip()
    {
        float uprightness = Vector3.Dot(transform.up, Vector3.up);

        if (uprightness < stabilityThreshold)
        {
            float submerged = Mathf.Clamp01(1 - uprightness) * submergedFactor;
            rb.AddForce(Vector3.up * uprightForce * submerged, ForceMode.Acceleration);

            Vector3 predictedUp = Quaternion.AngleAxis(
                rb.angularVelocity.magnitude * Mathf.Rad2Deg * 0.1f,
                rb.angularVelocity
            ) * transform.up;

            Vector3 torqueVector = Vector3.Cross(predictedUp, Vector3.up);
            rb.AddTorque(torqueVector * uprightTorque * (1 - uprightness), ForceMode.Acceleration);
        }
    }

    void AdjustDrag()
    {
        float tiltFactor = 1 - Vector3.Dot(transform.up, Vector3.up);
        rb.drag = originalDrag + tiltFactor * 2f;
        rb.angularDrag = originalAngularDrag + tiltFactor * 5f;
    }

    void OnDrawGizmosSelected()
    {
        if (buoyancyPoints != null)
        {
            Gizmos.color = Color.cyan;
            foreach (Transform point in buoyancyPoints)
            {
                Gizmos.DrawSphere(point.position, 0.3f);
            }

            Gizmos.color = Color.blue;
            Gizmos.DrawWireCube(
                new Vector3(transform.position.x, waterLevel, transform.position.z),
                new Vector3(10, 0.1f, 10)
            );
        }
    }

    void CheckCapsized()
    {
        float uprightness = Vector3.Dot(transform.up, Vector3.up);

        if (uprightness < capsizedThreshold)
        {
            capsizedTimer += Time.fixedDeltaTime;

            if (capsizedTimer >= recoveryDelay && !isCapsized)
            {
                StartCoroutine(EmergencyRecovery());
                isCapsized = true;
            }
        }
        else
        {
            capsizedTimer = 0f;
            isCapsized = false;
        }
    }

    IEnumerator EmergencyRecovery()
    {
        float duration = 5f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float progress = elapsed / duration;
            Vector3 torque = Vector3.Cross(transform.up, Vector3.up) *
                             emergencyTorque * (1 - progress);

            rb.AddTorque(torque, ForceMode.Acceleration);
            rb.AddForce(Vector3.up * uprightForce * 2f, ForceMode.Acceleration);

            elapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        isCapsized = false;
    }
}
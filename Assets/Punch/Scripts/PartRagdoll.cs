using UnityEngine;
using UnityEngine.Events;


public class PartRagdoll : MonoBehaviour
{
    private UnityAction<bool> _ragdollAction;
    private Rigidbody _rigidbody;
    private float _forcePunch;
    public void Initialize(UnityAction<bool> ragdollAction, float forcePunch)
    {
        _ragdollAction = ragdollAction;
        _forcePunch = forcePunch;
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void TakeDamage(bool isActive)
    {
        _ragdollAction?.Invoke(isActive);
    }

    public void TakePunch(ContactPoint contact)
    {
        Vector3 forceDirection = -contact.normal.normalized;
        _rigidbody.AddForceAtPosition(forceDirection * _forcePunch, contact.point, ForceMode.Impulse);
    }
}

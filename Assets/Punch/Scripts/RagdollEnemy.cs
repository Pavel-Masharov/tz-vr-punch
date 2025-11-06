using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollEnemy : MonoBehaviour
{
    [SerializeField] private Animator _animator;
    [SerializeField] private float _forsePunch = 1000;
    private Rigidbody[] _rigidbodies;


    private void Start()
    {
        Initialize();
    }

    public void Initialize()
    {
        FillRigidbodies();
        ToggleRagdoll(true);
    }

    public void FillRigidbodies()
    {
        _rigidbodies = GetComponentsInChildren<Rigidbody>();
        foreach (var rigidbody in _rigidbodies)
        {
             rigidbody.gameObject.AddComponent<PartRagdoll>().Initialize(ToggleRagdoll, _forsePunch);
        }
    }

    public void ToggleRagdoll(bool isActivate)
    {
        _animator.enabled = isActivate;

        foreach (var rigidbody in _rigidbodies)
        {
            rigidbody.isKinematic = isActivate;
        }
           
    }
}

using BNG;
using UnityEngine;

public class ShaderDamageCollider : DamageCollider
{
    [Header("Shader Damage Settings")]
    public bool showDebug = true;
    public float forceMultiplier = 2f;

    void OnCollisionEnter(Collision collision)
    {
        try
        {
            float impactForce = collision.relativeVelocity.magnitude;
            HeadDamageShader headDamage = collision.gameObject.GetComponent<HeadDamageShader>();
            if (headDamage != null && impactForce >= MinForce)
            {
                ContactPoint contact = collision.contacts[0];
                Vector3 hitPoint = contact.point;
                Vector3 hitNormal = contact.normal;

                float calculatedDamage = Damage * (impactForce * forceMultiplier);
                headDamage.TakeDamageAtPoint(calculatedDamage, hitPoint, hitNormal, impactForce);
            }


            PartRagdoll partRagdoll = collision.gameObject.GetComponent<PartRagdoll>();
            if (partRagdoll != null && impactForce >= MinForce)
            {
                ContactPoint contact = collision.contacts[0];
                partRagdoll.TakeDamage(false);
                partRagdoll.TakePunch(contact);
            }

        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error in ShaderDamageCollider: {e.Message}");
        }
    }
}
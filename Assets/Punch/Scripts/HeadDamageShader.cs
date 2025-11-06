using BNG;
using UnityEngine;
using System.Collections;

public class HeadDamageShader : Damageable
{
    [Header("Shader Damage System")]
    public Renderer headRenderer;
    public Texture2D damageMaskTexture;
    public Color damageColor = new Color(0.8f, 0.2f, 0.3f, 1f);
    public float damageIntensity = 0.8f;

    [Header("Brush Settings")]
    public float brushSize = 0.1f;
    public AnimationCurve damageFalloff = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Deformation Settings")]
    public float deformationStrength = 0.05f;
    public float deformationRadiusMultiplier = 1.5f;
    public bool permanentDeformation = false;

    [Header("Effects")]
    public ParticleSystem bloodParticles;
    public AudioClip[] hitSounds;

    [SerializeField] private bool _isApplyDeformation = true;
    [SerializeField] private bool _isApplyColor = true;


    private Texture2D dynamicMask;
    private Texture2D deformationMask;
    private Material headMaterial;
    private AudioSource audioSource;
    private Mesh originalMesh;
    private Mesh deformedMesh;
    private Vector3[] originalVertices;


    private static int MaskTextureID = Shader.PropertyToID("_DamageMask");
    private static int DamageColorID = Shader.PropertyToID("_DamageColor");
    private static int DeformationMaskID = Shader.PropertyToID("_DeformationMask");
    private static int DeformationStrengthID = Shader.PropertyToID("_DeformationStrength");

    void Start()
    {
        headMaterial = headRenderer.material;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }

        InitializeDamageSystem();
        InitializeDeformationSystem();
        Health = 100f;
    }

    void InitializeDamageSystem()
    {
        dynamicMask = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        dynamicMask.wrapMode = TextureWrapMode.Clamp;
        dynamicMask.filterMode = FilterMode.Bilinear;

        ClearDamageMask();

        headMaterial.SetTexture(MaskTextureID, dynamicMask);
        headMaterial.SetColor(DamageColorID, damageColor);
    }

    void InitializeDeformationSystem()
    {
        deformationMask = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        deformationMask.wrapMode = TextureWrapMode.Clamp;
        deformationMask.filterMode = FilterMode.Bilinear;

        ClearDeformationMask();

        headMaterial.SetTexture(DeformationMaskID, deformationMask);
        headMaterial.SetFloat(DeformationStrengthID, deformationStrength);

        MeshFilter meshFilter = GetComponent<MeshFilter>();
        SkinnedMeshRenderer skinnedRenderer = GetComponent<SkinnedMeshRenderer>();

        if (skinnedRenderer != null)
        {
            originalMesh = skinnedRenderer.sharedMesh;
            deformedMesh = Instantiate(originalMesh);
            skinnedRenderer.sharedMesh = deformedMesh;
            originalVertices = originalMesh.vertices;
        }
        else if (meshFilter != null)
        {
            originalMesh = meshFilter.mesh;
            deformedMesh = Instantiate(originalMesh);
            meshFilter.mesh = deformedMesh;
            originalVertices = originalMesh.vertices;
        }

    }

    public void TakeDamageAtPoint(float damage, Vector3 hitPoint, Vector3 hitNormal, float impactForce)
    {
        DealDamage(damage);


        if(_isApplyColor)
            AddDamageToMask(hitPoint, impactForce);

        if (_isApplyDeformation)
            AddDeformation(hitPoint, hitNormal, impactForce);

        SpawnBloodParticles(hitPoint, hitNormal);
        PlayHitSound(impactForce);

    }

    void AddDamageToMask(Vector3 worldPosition, float force)
    {
        Vector2 uv = WorldToUV(worldPosition);

        if (IsValidUV(uv))
        {
            Vector2 pixelPos = new Vector2(uv.x * dynamicMask.width, uv.y * dynamicMask.height);
            float radius = brushSize * dynamicMask.width * Mathf.Clamp(force / 3f, 0.5f, 2f);

            DrawBrush(dynamicMask, (int)pixelPos.x, (int)pixelPos.y, (int)radius, damageIntensity);
            dynamicMask.Apply();

        }
    }

    void AddDeformation(Vector3 worldPosition, Vector3 hitNormal, float force)
    {
        Vector2 uv = WorldToUV(worldPosition);

        if (IsValidUV(uv))
        {
            Vector2 pixelPos = new Vector2(uv.x * deformationMask.width, uv.y * deformationMask.height);
            float radius = brushSize * deformationMask.width * Mathf.Clamp(force / 3f, 0.5f, 2f) * deformationRadiusMultiplier;
            float intensity = Mathf.Clamp(force / 10f, 0.1f, 1f);

            DrawBrush(deformationMask, (int)pixelPos.x, (int)pixelPos.y, (int)radius, intensity);
            deformationMask.Apply();

            if (!permanentDeformation)
            {
                DeformMesh(worldPosition, hitNormal, force);
            }

        }
    }

    void DeformMesh(Vector3 worldPosition, Vector3 hitNormal, float force)
    {
        if (deformedMesh == null || originalVertices == null) return;

        Vector3[] vertices = deformedMesh.vertices;
        Vector3 localHitPoint = transform.InverseTransformPoint(worldPosition);
        Vector3 localHitNormal = transform.InverseTransformDirection(-hitNormal);

        float deformationIntensity = Mathf.Clamp(force / 15f, 0.01f, 0.1f) * deformationStrength;
        float radius = brushSize * Mathf.Clamp(force / 3f, 0.5f, 2f);

        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(vertices[i], localHitPoint);
            if (distance <= radius)
            {
                float falloff = 1f - (distance / radius);
                falloff = damageFalloff.Evaluate(falloff);

                vertices[i] += localHitNormal * deformationIntensity * falloff;
            }
        }

        deformedMesh.vertices = vertices;
        deformedMesh.RecalculateNormals();
        deformedMesh.RecalculateBounds();
    }

    void DrawBrush(Texture2D tex, int centerX, int centerY, int radius, float intensity)
    {
        Color[] pixels = tex.GetPixels();

        for (int y = -radius; y <= radius; y++)
        {
            for (int x = -radius; x <= radius; x++)
            {
                float distance = Mathf.Sqrt(x * x + y * y) / radius;
                if (distance <= 1.0f)
                {
                    int texX = centerX + x;
                    int texY = centerY + y;

                    if (texX >= 0 && texX < tex.width && texY >= 0 && texY < tex.height)
                    {
                        int pixelIndex = texY * tex.width + texX;
                        float falloff = damageFalloff.Evaluate(distance);
                        float currentValue = pixels[pixelIndex].r;
                        float newValue = Mathf.Max(currentValue, intensity * falloff);

                        pixels[pixelIndex] = new Color(newValue, newValue, newValue, 1f);
                    }
                }
            }
        }

        tex.SetPixels(pixels);
    }

    void ClearDamageMask()
    {
        Color[] clearPixels = new Color[dynamicMask.width * dynamicMask.height];
        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = Color.black;
        }
        dynamicMask.SetPixels(clearPixels);
        dynamicMask.Apply();
    }

    void ClearDeformationMask()
    {
        Color[] clearPixels = new Color[deformationMask.width * deformationMask.height];
        for (int i = 0; i < clearPixels.Length; i++)
        {
            clearPixels[i] = Color.black;
        }
        deformationMask.SetPixels(clearPixels);
        deformationMask.Apply();
    }

    Vector2 WorldToUV(Vector3 worldPosition)
    {
        SkinnedMeshRenderer skinnedRenderer = GetComponent<SkinnedMeshRenderer>();
        MeshFilter meshFilter = GetComponent<MeshFilter>();

        if (skinnedRenderer != null)
        {
            return WorldToUV_Skinned(worldPosition, skinnedRenderer);
        }
        else if (meshFilter != null && meshFilter.mesh != null)
        {
            return WorldToUV_Static(worldPosition, meshFilter);
        }

        return Vector2.zero;
    }

    Vector2 WorldToUV_Skinned(Vector3 worldPosition, SkinnedMeshRenderer skinnedRenderer)
    {
        Mesh bakedMesh = new Mesh();
        skinnedRenderer.BakeMesh(bakedMesh);

        Vector3[] vertices = bakedMesh.vertices;
        Vector2[] uvs = bakedMesh.uv;

        Vector3 localHitPoint = transform.InverseTransformPoint(worldPosition);

        int closestVertexIndex = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 worldVertex = transform.TransformPoint(vertices[i]);
            float distance = Vector3.Distance(worldVertex, worldPosition);

            if (distance < minDistance)
            {
                minDistance = distance;
                closestVertexIndex = i;
            }
        }

        if (closestVertexIndex >= 0 && closestVertexIndex < uvs.Length)
        {
            Vector2 uv = uvs[closestVertexIndex];
            DestroyImmediate(bakedMesh);
            return uv;
        }

        DestroyImmediate(bakedMesh);
        return Vector2.zero;
    }

    Vector2 WorldToUV_Static(Vector3 worldPosition, MeshFilter meshFilter)
    {
        Mesh mesh = meshFilter.mesh;
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = mesh.uv;

        Vector3 localHitPoint = transform.InverseTransformPoint(worldPosition);

        int closestVertexIndex = -1;
        float minDistance = float.MaxValue;

        for (int i = 0; i < vertices.Length; i++)
        {
            float distance = Vector3.Distance(vertices[i], localHitPoint);
            if (distance < minDistance)
            {
                minDistance = distance;
                closestVertexIndex = i;
            }
        }

        if (closestVertexIndex >= 0 && closestVertexIndex < uvs.Length)
        {
            Vector2 uv = uvs[closestVertexIndex];
            return uv;
        }

        return Vector2.zero;
    }

    bool IsValidUV(Vector2 uv)
    {
        return uv.x >= 0 && uv.x <= 1 && uv.y >= 0 && uv.y <= 1;
    }

    void SpawnBloodParticles(Vector3 position, Vector3 normal)
    {
        if (bloodParticles != null)
        {
            ParticleSystem particles = Instantiate(bloodParticles, position, Quaternion.LookRotation(normal));
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration);
        }
    }

    void PlayHitSound(float force)
    {
        if (hitSounds != null && hitSounds.Length > 0)
        {
            AudioClip sound = hitSounds[Random.Range(0, hitSounds.Length)];
            audioSource.pitch = Random.Range(0.9f, 1.1f);
            audioSource.volume = Mathf.Clamp(force / 5f, 0.1f, 1f);
            audioSource.PlayOneShot(sound);
        }
    }

    public override void DestroyThis()
    {
        ClearDamageMask();
        ClearDeformationMask();
        base.DestroyThis();
    }
}
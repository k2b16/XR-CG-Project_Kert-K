using Unity.XR.CoreUtils.GUI;
using UnityEngine;

public class ParticleControll : MonoBehaviour
{
    [Header("Particles")]
    public ParticleSystem particles;
    public int particleCount = 200;
    public float radius = 1.5f;
    
    private ParticleSystem.MainModule mainModule;
    private ParticleSystem.ShapeModule shapeModule;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        particles = GetComponent<ParticleSystem>();
        mainModule = particles.main;
        shapeModule = particles.shape;
        ApplySettings();
    }

    public void ApplySettings()
    {
        mainModule.maxParticles = particleCount;
        shapeModule.radius = radius;
    }

    public void SetRadius(float newRadius)
    {
        radius = newRadius;
        shapeModule.radius = radius;
    }

    public void SetCount(int newCount)
    {
        particleCount = newCount;
        mainModule.maxParticles = particleCount;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

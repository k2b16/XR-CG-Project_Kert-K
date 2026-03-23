using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class ParticleInteract : MonoBehaviour
{
    [Header("References")]
    public ParticleSystem particles;
    public Transform controllerTransform;

    [Header("Interaction Settings")]
    public float influenceRadius = 0.5f;
    public float influenceStrenght = 2.0f;

    private ParticleSystem.Particle[] _particleArray;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Controller pos:" + controllerTransform.position);
        if (particles == null || controllerTransform == null) return;
        int count = particles.particleCount;
        if (_particleArray == null || _particleArray.Length < count) _particleArray = new ParticleSystem.Particle[count];

        particles.GetParticles(_particleArray, count );
        for (int i = 0; i < count; i++) {
            Vector3 toParticle = _particleArray[i].position - controllerTransform.position;
            float distance = toParticle.magnitude;
            if (distance < influenceStrenght && distance > 0.01f)
            {
                float force = (1f - (distance / influenceRadius)) * influenceStrenght;
                _particleArray[i].velocity += toParticle.normalized * force * Time.deltaTime;
            }
        }
        particles.SetParticles(_particleArray, count);
    }
}

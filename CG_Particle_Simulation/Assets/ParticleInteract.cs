using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit;

public class ParticleInteract : MonoBehaviour
{
    [Header("References")]
    public ParticleSystem particles;
    public Transform controllerTf;

    [Header("Interaction")]
    public float infRadius = 1.5f;
    public float infStrenght = 100f;

    private ParticleSystem.Particle[] particleArr;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log("Controller pos:" + controllerTf.position);
        if (particles == null || controllerTf == null) return;
        int count = particles.particleCount;
        if (particleArr == null || particleArr.Length < count) particleArr = new ParticleSystem.Particle[count];

        particles.GetParticles(particleArr, count );
        for (int i = 0; i < count; i++) {
            Vector3 toParticle = particleArr[i].position - controllerTf.position;
            float distance = toParticle.magnitude;
            if (distance < infRadius && distance > 0.01f)
            {
                float force = (1f - (distance / infRadius)) * infStrenght;
                Vector3 flyDir = toParticle.normalized;
                particleArr[i].velocity = flyDir * force;
                particleArr[i].remainingLifetime = Mathf.Max(particleArr[i].remainingLifetime, 2f);
            }
        }
        particles.SetParticles(particleArr, count);
    }
    void OnDrawGizmos()
    {
        if (controllerTf == null) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(controllerTf.position, infRadius);
    }
}

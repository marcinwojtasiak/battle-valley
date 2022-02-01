using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeathAnimation : MonoBehaviour
{
    [SerializeField] private GameObject deathParticleSystem;

    public void PlayAnimation(Sprite sprite, Vector3 attackDirection)
    {
        GameObject deathObject = Instantiate(deathParticleSystem);
        deathObject.transform.position = transform.position;
        deathObject.transform.localScale = new Vector3(transform.lossyScale.x, transform.lossyScale.x, transform.lossyScale.y);

        ParticleSystem deathSystem = deathObject.GetComponent<ParticleSystem>();

        float pixelsPerUnit = sprite.pixelsPerUnit;
        int spritePixelSize = sprite.texture.height;
        float speedMultiplier = 0.3f;

        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[spritePixelSize * spritePixelSize];

        for (int i = 0; i < spritePixelSize; i++)
        {
            for (int j = 0; j < spritePixelSize; j++)
            {
                ParticleSystem.Particle particle = new ParticleSystem.Particle();
                Color color = sprite.texture.GetPixel(i, j);
                if (color.a == 0)
                    continue;
                particle.startColor = color;
                particle.startSize = 1 / pixelsPerUnit * 1;
                particle.startLifetime = 1.5f;
                particle.position = new Vector3(i / pixelsPerUnit - spritePixelSize / pixelsPerUnit / 2, 0, j / pixelsPerUnit - spritePixelSize / pixelsPerUnit / 2);
                particle.remainingLifetime = Random.Range(1f, 1.5f);

                particle.velocity = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)) + attackDirection;

                particle.velocity *= speedMultiplier;
                particles[spritePixelSize * i + j] = particle;
            }
        }

        deathSystem.SetParticles(particles);

        Destroy(deathObject, 2f);
    }
}

using System.Collections.Generic;
using UnityEngine;

public class SurvivorOrbitWeapon : MonoBehaviour
{
    private SurvivorMinigameController controller;
    private Transform[] orbitNodes;
    private float orbitAngle;
    private float radius;
    private float speed;
    private float damage;
    private float hitRadius;
    private readonly Collider[] overlapBuffer = new Collider[16];
    private readonly Dictionary<SurvivorMinigameEnemy, float> hitCooldowns = new Dictionary<SurvivorMinigameEnemy, float>();
    private const float HitInterval = 0.2f;

    public void Initialize(
        SurvivorMinigameController owner,
        int count,
        float orbitRadius,
        float orbitSpeed,
        float orbitDamage,
        float hitRadius)
    {
        controller = owner;
        radius = orbitRadius;
        speed = orbitSpeed;
        damage = orbitDamage;
        this.hitRadius = hitRadius;

        BuildOrbitNodes(count);
    }

    private void BuildOrbitNodes(int count)
    {
        ClearOrbitNodes();

        orbitNodes = new Transform[count];
        for (int i = 0; i < count; i++)
        {
            GameObject node = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            node.name = $"OrbitBlade_{i}";
            node.transform.SetParent(transform, false);
            node.transform.localScale = Vector3.one * hitRadius * 2f;

            Collider col = node.GetComponent<Collider>();
            if (col != null)
                col.isTrigger = true;

            Rigidbody rb = node.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            SurvivorOrbitHitbox hitbox = node.AddComponent<SurvivorOrbitHitbox>();
            hitbox.Initialize(this, damage);

            Renderer renderer = node.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.4f, 0.85f, 1f);

            orbitNodes[i] = node.transform;
        }
    }

    private void Update()
    {
        if (orbitNodes == null || orbitNodes.Length == 0)
            return;

        orbitAngle += speed * Time.deltaTime;

        for (int i = 0; i < orbitNodes.Length; i++)
        {
            float angle = orbitAngle + (360f / orbitNodes.Length) * i;
            float rad = angle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * radius;
            orbitNodes[i].localPosition = offset + Vector3.up * 0.6f;
            DamageAtPoint(orbitNodes[i].position);
        }
    }

    private void DamageAtPoint(Vector3 worldPoint)
    {
        float now = Time.time;
        int hitCount = Physics.OverlapSphereNonAlloc(worldPoint, hitRadius, overlapBuffer);
        for (int i = 0; i < hitCount; i++)
        {
            SurvivorMinigameEnemy enemy = overlapBuffer[i].GetComponent<SurvivorMinigameEnemy>();
            if (enemy == null)
                continue;

            if (hitCooldowns.TryGetValue(enemy, out float nextHitTime) && now < nextHitTime)
                continue;

            hitCooldowns[enemy] = now + HitInterval;
            DamageEnemy(enemy);
        }
    }

    public void DamageEnemy(SurvivorMinigameEnemy enemy)
    {
        if (enemy == null || controller == null || !controller.IsRunning)
            return;

        enemy.TakeDamage(damage);
    }

    private void ClearOrbitNodes()
    {
        if (orbitNodes == null)
            return;

        for (int i = 0; i < orbitNodes.Length; i++)
        {
            if (orbitNodes[i] != null)
                Destroy(orbitNodes[i].gameObject);
        }

        orbitNodes = null;
    }

    private void OnDestroy()
    {
        ClearOrbitNodes();
    }
}

public class SurvivorOrbitHitbox : MonoBehaviour
{
    private SurvivorOrbitWeapon weapon;
    private float damage;

    public void Initialize(SurvivorOrbitWeapon owner, float hitDamage)
    {
        weapon = owner;
        damage = hitDamage;
    }

    private void OnTriggerEnter(Collider other)
    {
        SurvivorMinigameEnemy enemy = other.GetComponent<SurvivorMinigameEnemy>();
        if (enemy != null)
            weapon.DamageEnemy(enemy);
    }
}

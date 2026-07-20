using UnityEngine;

/// <summary>
/// Local invincible forcefield while the upgrade / starter-weapon menu is open.
/// World keeps running; this only protects and gently pushes enemies away.
/// </summary>
public class SurvivorPauseShield : MonoBehaviour
{
    public float radius = 6f;
    public float pushStrength = 18f;

    private Transform followTarget;
    private GameObject visual;
    private SphereCollider trigger;

    public static SurvivorPauseShield Attach(Transform player, float shieldRadius = 6f)
    {
        GameObject go = new GameObject("SurvivorPauseShield");
        SurvivorPauseShield shield = go.AddComponent<SurvivorPauseShield>();
        shield.followTarget = player;
        shield.radius = shieldRadius;
        shield.Build();
        return shield;
    }

    private void Build()
    {
        transform.position = followTarget != null ? followTarget.position : Vector3.zero;

        visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        visual.name = "ShieldVisual";
        visual.transform.SetParent(transform, false);
        visual.transform.localScale = Vector3.one * radius * 2f;
        Object.Destroy(visual.GetComponent<Collider>());

        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material = SurvivorTransparentMaterial.Create(new Color(0.45f, 0.85f, 1f), 0.28f);

        trigger = gameObject.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius = radius;
    }

    private void LateUpdate()
    {
        if (followTarget == null)
            return;

        transform.position = followTarget.position;
        if (visual != null)
            visual.transform.localScale = Vector3.one * radius * 2f;
        if (trigger != null)
            trigger.radius = radius;
    }

    private void OnTriggerStay(Collider other)
    {
        if (other == null)
            return;

        SurvivorMinigameEnemy enemy = other.GetComponentInParent<SurvivorMinigameEnemy>();
        if (enemy == null)
            return;

        Vector3 away = enemy.transform.position - transform.position;
        away.y = 0f;
        if (away.sqrMagnitude < 0.01f)
            away = Vector3.forward;

        enemy.ApplyKnockback(away.normalized, pushStrength * Time.deltaTime);
    }

    public void Teardown()
    {
        Destroy(gameObject);
    }
}

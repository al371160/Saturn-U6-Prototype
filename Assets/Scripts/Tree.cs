using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Tree : MonoBehaviour
{
    [Header("Tree Settings")]
    public int maxHealth = 3;
    public List<GameObject> woodPrefabs;
    public Transform dropPoint;
    public float dropForce = 5f;
    public float shakeDuration = 0.2f;
    public float shakeMagnitude = 0.1f;

    public int currentHealth;

    private Vector3 originalPosition;
    private Coroutine shakeCoroutine;

    private void Start()
    {
        currentHealth = maxHealth;
        originalPosition = transform.localPosition;
    }

    public void TakeDamage(int amount)
    {
        DropWood();
        currentHealth -= amount;
        Debug.Log("tree hit");

        // Shake the tree
        if (shakeCoroutine != null) StopCoroutine(shakeCoroutine);
        shakeCoroutine = StartCoroutine(ShakeTree());

        if (currentHealth <= 0)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator ShakeTree()
    {
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float x = Random.Range(-1f, 1f) * shakeMagnitude;
            float z = Random.Range(-1f, 1f) * shakeMagnitude;

            transform.localPosition = originalPosition + new Vector3(x, 0, z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPosition;
    }

    private void DropWood()
    {
        foreach (GameObject woodPrefab in woodPrefabs)
        {
            GameObject wood = Instantiate(woodPrefab, dropPoint.position, Quaternion.identity);
            Rigidbody rb = wood.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 launchDir = new Vector3(Random.Range(-1f, 1f), 1f, Random.Range(-1f, 1f)).normalized;
                rb.AddForce(launchDir * dropForce, ForceMode.Impulse);
            }

            Item itemScript = wood.GetComponent<Item>();
            if (itemScript != null)
            {
                itemScript.enabled = false;
                StartCoroutine(EnableItemAfterDelay(itemScript, 0.1f));
            }
        }
    }

    private IEnumerator EnableItemAfterDelay(Item item, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (item != null)
            item.enabled = true;
    }
}

using UnityEngine;
using Unity.Mathematics;
using UnityEngine.Splines;
using System.Collections.Generic;

[ExecuteInEditMode]
public class WaterStreamGenerator : MonoBehaviour
{
    public SplineContainer splineContainer;
    public float width = 2f;
    public int segmentsPerCurve = 10;
    public GameObject particlePrefab;

    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();
    }

    [ContextMenu("Generate Water Stream")]
    public void Generate()
    {
        EnsureComponents();

        if (splineContainer == null || splineContainer.Spline.Count < 2)
        {
            Debug.LogWarning("Invalid spline setup.");
            return;
        }

        GenerateMesh();
        
        if (Application.isPlaying)
        {
            GenerateParticles();
        }
    }

    public void GenerateMesh()
    {
        if (splineContainer == null || splineContainer.Spline.Count < 2)
        {
            Debug.LogWarning("Invalid spline setup.");
            return;
        }

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        Spline spline = splineContainer.Spline;
        float totalLength = spline.GetLength();
        float step = 1f / (spline.Count * segmentsPerCurve);

        int vertIndex = 0;

        for (float t = 0f; t < 1f; t += step)
        {
            Vector3 position = spline.EvaluatePosition(t);
            Vector3 forward = ((Vector3)math.normalize(spline.EvaluateTangent(t)));
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            Vector3 leftPoint = position - right * (width * 0.5f);
            Vector3 rightPoint = position + right * (width * 0.5f);

            vertices.Add(leftPoint);
            vertices.Add(rightPoint);
            uvs.Add(new Vector2(0, t));
            uvs.Add(new Vector2(1, t));

            if (vertIndex >= 2)
            {
                triangles.Add(vertIndex - 2);
                triangles.Add(vertIndex - 1);
                triangles.Add(vertIndex + 1);

                triangles.Add(vertIndex - 2);
                triangles.Add(vertIndex + 1);
                triangles.Add(vertIndex);
            }

            vertIndex += 2;
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();

        meshFilter.mesh = mesh;

        GenerateParticles();
    }

    private void GenerateParticles()
    {
        // Clear existing
        foreach (Transform child in transform)
        {
            if (Application.isEditor)
                DestroyImmediate(child.gameObject);
            else
                Destroy(child.gameObject);
        }

        if (particlePrefab == null) return;

        var spline = splineContainer.Spline;
        for (int i = 0; i < spline.Count; i++)
        {
            Vector3 pos = spline[i].Position;
            GameObject particle = Instantiate(particlePrefab, transform);
            particle.transform.localPosition = pos;
        }
    }

    private void EnsureComponents()
    {
        if (meshFilter == null)
            meshFilter = GetComponent<MeshFilter>() ?? gameObject.AddComponent<MeshFilter>();

        if (meshRenderer == null)
            meshRenderer = GetComponent<MeshRenderer>() ?? gameObject.AddComponent<MeshRenderer>();

        if (meshRenderer.sharedMaterial == null)
            meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
    }
    }

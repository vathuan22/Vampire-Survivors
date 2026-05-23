using UnityEngine;

public class LightningEffectRenderer : MonoBehaviour
{
    private LineRenderer lineRenderer;
    public float lifetime = 0.2f;
    private float timer;
    
    void Start()
    {
        // Tự động thêm LineRenderer nếu chưa có
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
            lineRenderer = gameObject.AddComponent<LineRenderer>();
        
        // Setup LineRenderer
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.startColor = Color.yellow;
        lineRenderer.endColor = new Color(1, 1, 0, 0); // Fade to transparent
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.1f;
        lineRenderer.sortingOrder = 5;
        
        timer = lifetime;
    }
    
    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0)
            Destroy(gameObject);
    }
    
    public void DrawLightning(Vector3 startPos, Vector3 endPos, int segments = 5)
    {
        // ✅ Ensure lineRenderer exists
        if (lineRenderer == null)
        {
            lineRenderer = GetComponent<LineRenderer>();
            if (lineRenderer == null)
                lineRenderer = gameObject.AddComponent<LineRenderer>();
            
            // Setup if newly created
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            if (lineRenderer.material == null)
                lineRenderer.material = new Material(Shader.Find("Standard"));
            
            lineRenderer.startColor = Color.yellow;
            lineRenderer.endColor = new Color(1, 1, 0, 0);
            lineRenderer.startWidth = 0.2f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.sortingOrder = 5;
        }
        
        Vector3[] points = GenerateZigzagPath(startPos, endPos, segments);
        
        lineRenderer.positionCount = points.Length;
        for (int i = 0; i < points.Length; i++)
        {
            lineRenderer.SetPosition(i, points[i]);
        }
    }
    
    Vector3[] GenerateZigzagPath(Vector3 start, Vector3 end, int segments)
    {
        Vector3[] points = new Vector3[segments + 1];
        points[0] = start;
        points[segments] = end;
        
        // Tạo zigzag effect
        Vector3 direction = (end - start).normalized;
        Vector3 perpendicular = new Vector3(-direction.y, direction.x, 0); // Vuông góc
        float distance = Vector3.Distance(start, end);
        
        for (int i = 1; i < segments; i++)
        {
            float t = (float)i / segments;
            Vector3 basePoint = Vector3.Lerp(start, end, t);
            
            // Thêm ngẫu nhiênOffset
            float randomOffset = Random.Range(-distance * 0.1f, distance * 0.1f);
            points[i] = basePoint + perpendicular * randomOffset;
        }
        
        return points;
    }
}

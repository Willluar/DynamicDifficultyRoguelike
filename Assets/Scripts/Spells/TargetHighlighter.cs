using UnityEngine;

public class TargetHighlighter : MonoBehaviour
{
    private SpriteRenderer[] spriteRenderers;
    private Color[] originalColors;

    [Header("Highlight Settings")]
    public Color highlightedColor = new Color(1f, 0.4f, 1f, 1f);

    private void Awake()
    {
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);

        originalColors = new Color[spriteRenderers.Length];

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            originalColors[i] = spriteRenderers[i].color;
        }
    }

    public void SetHighlighted(bool highlighted)
    {
        if (spriteRenderers == null || spriteRenderers.Length == 0)
            return;

        for (int i = 0; i < spriteRenderers.Length; i++)
        {
            spriteRenderers[i].color = highlighted ? highlightedColor : originalColors[i];
        }
    }
}
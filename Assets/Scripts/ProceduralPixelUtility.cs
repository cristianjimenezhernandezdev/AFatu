using UnityEngine;

public static class ProceduralPixelUtility
{
    private static Sprite cachedSquareSprite;

    public static Sprite GetOrCreateSquareSprite()
    {
        if (cachedSquareSprite != null)
            return cachedSquareSprite;

        Texture2D texture = new(1, 1);
        texture.filterMode = FilterMode.Point;
        texture.SetPixel(0, 0, Color.white);
        texture.Apply();

        cachedSquareSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, 1f, 1f),
            new Vector2(0.5f, 0.5f),
            1f
        );

        return cachedSquareSprite;
    }

    public static void DestroyGeneratedChildren(Transform parent, string prefix = "Proc_")
    {
        if (parent == null)
            return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (!string.IsNullOrEmpty(prefix) && !child.name.StartsWith(prefix))
                continue;

            if (Application.isPlaying)
                Object.Destroy(child.gameObject);
            else
                Object.DestroyImmediate(child.gameObject);
        }
    }

    public static GameObject CreatePixel(Transform parent, string name, Vector3 localPosition, float size, Color color, int sortingOrder)
    {
        GameObject pixel = new(name);
        pixel.transform.SetParent(parent, false);
        pixel.transform.localPosition = localPosition;
        pixel.transform.localScale = Vector3.one * size;

        SpriteRenderer spriteRenderer = pixel.AddComponent<SpriteRenderer>();
        spriteRenderer.sprite = GetOrCreateSquareSprite();
        spriteRenderer.color = color;
        spriteRenderer.sortingOrder = sortingOrder;

        return pixel;
    }

    public static int Hash(int x, int y, int seed = 0)
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 31 + x;
            hash = hash * 31 + y;
            hash = hash * 31 + seed;
            return hash;
        }
    }

    public static Color Multiply(Color color, float factor)
    {
        return new Color(
            Mathf.Clamp01(color.r * factor),
            Mathf.Clamp01(color.g * factor),
            Mathf.Clamp01(color.b * factor),
            color.a
        );
    }

    public static Color Blend(Color a, Color b, float t)
    {
        return Color.Lerp(a, b, Mathf.Clamp01(t));
    }
}

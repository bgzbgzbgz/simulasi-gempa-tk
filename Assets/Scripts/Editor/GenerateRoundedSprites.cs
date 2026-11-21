using System.IO;
using UnityEditor;
using UnityEngine;

public static class RoundedSpriteGenerator
{
    public static void GenerateAll()
    {
        string dir = "Assets/Sprites/UI";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        Color darkBorder = new Color(0.11f, 0.15f, 0.22f, 1f); // #1C2638
        Color greenFill = new Color(0.13f, 0.77f, 0.36f, 1f);  // #22C55E / Upin-Ipin green
        Color whiteFill = Color.white;
        Color yellowFill = new Color(0.98f, 0.75f, 0.14f, 1f); // #FBBF24
        Color redFill = new Color(0.94f, 0.27f, 0.24f, 1f);    // #EF4444
        Color orangeFill = new Color(0.96f, 0.62f, 0.13f, 1f); // #F59E0B
        Color darkHudFill = new Color(0.12f, 0.16f, 0.22f, 0.92f); // HUD dark
        Color translucentCard = new Color(1f, 1f, 1f, 0.95f);
        Color subtleBorder = new Color(0.82f, 0.85f, 0.90f, 1f);

        CreateAndSaveSprite($"{dir}/spr_ui_rounded_fill.png", 128, 26f, 0f, Color.white, Color.white, 36);
        CreateAndSaveSprite($"{dir}/spr_ui_rounded_btn_white.png", 128, 24f, 4f, darkBorder, whiteFill, 36);
        CreateAndSaveSprite($"{dir}/spr_ui_rounded_btn_green.png", 128, 24f, 4f, darkBorder, greenFill, 36);
        CreateAndSaveSprite($"{dir}/spr_ui_rounded_btn_yellow.png", 128, 24f, 4f, darkBorder, yellowFill, 36);
        CreateAndSaveSprite($"{dir}/spr_ui_rounded_btn_red.png", 128, 22f, 3.5f, darkBorder, redFill, 34);
        CreateAndSaveSprite($"{dir}/spr_ui_rounded_btn_orange.png", 128, 22f, 3.5f, darkBorder, orangeFill, 34);
        CreateAndSaveSprite($"{dir}/spr_ui_rounded_card_parchment.png", 128, 26f, 4f, darkBorder, translucentCard, 36);
        CreateAndSaveSprite($"{dir}/spr_ui_rounded_card_subtle.png", 128, 24f, 3f, subtleBorder, whiteFill, 36);
        CreateAndSaveSprite($"{dir}/spr_ui_rounded_hud_dark.png", 128, 22f, 0f, Color.clear, darkHudFill, 34);

        AssetDatabase.Refresh();

        // Configure Sprite Importers
        ConfigureImporter($"{dir}/spr_ui_rounded_fill.png", 36);
        ConfigureImporter($"{dir}/spr_ui_rounded_btn_white.png", 36);
        ConfigureImporter($"{dir}/spr_ui_rounded_btn_green.png", 36);
        ConfigureImporter($"{dir}/spr_ui_rounded_btn_yellow.png", 36);
        ConfigureImporter($"{dir}/spr_ui_rounded_btn_red.png", 34);
        ConfigureImporter($"{dir}/spr_ui_rounded_btn_orange.png", 34);
        ConfigureImporter($"{dir}/spr_ui_rounded_card_parchment.png", 36);
        ConfigureImporter($"{dir}/spr_ui_rounded_card_subtle.png", 36);
        ConfigureImporter($"{dir}/spr_ui_rounded_hud_dark.png", 34);

        Debug.Log("[RoundedSpriteGenerator] All rounded sprites generated and configured successfully!");
    }

    private static void CreateAndSaveSprite(string path, int size, float radius, float borderWidth, Color borderColor, Color fillColor, int borderSlice)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float cx = (size - 1) * 0.5f;
        float cy = (size - 1) * 0.5f;
        float padding = 1.5f;
        float ex = cx - padding;
        float ey = cy - padding;

        Color[] pixels = new Color[size * size];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float qx = Mathf.Abs(x - cx) - (ex - radius);
                float qy = Mathf.Abs(y - cy) - (ey - radius);
                float dist = Mathf.Min(Mathf.Max(qx, qy), 0.0f) + new Vector2(Mathf.Max(qx, 0.0f), Mathf.Max(qy, 0.0f)).magnitude - radius;

                float outerAlpha = Mathf.Clamp01(0.5f - dist);

                if (borderWidth > 0.01f)
                {
                    float innerAlpha = Mathf.Clamp01(0.5f - (dist + borderWidth));
                    Color c = Color.Lerp(borderColor, fillColor, innerAlpha);
                    c.a = outerAlpha * Mathf.Max(borderColor.a, fillColor.a);
                    pixels[y * size + x] = c;
                }
                else
                {
                    Color c = fillColor;
                    c.a = outerAlpha * fillColor.a;
                    pixels[y * size + x] = c;
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        byte[] pngBytes = tex.EncodeToPNG();
        File.WriteAllBytes(path, pngBytes);
        Object.DestroyImmediate(tex);
    }

    private static void ConfigureImporter(string path, int border)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spriteBorder = new Vector4(border, border, border, border);
            importer.filterMode = FilterMode.Bilinear;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}

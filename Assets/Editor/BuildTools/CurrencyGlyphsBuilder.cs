using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// Playtest follow-up (2026-07-11) — inline currency glyphs: builds a tiny
    /// two-cell sprite sheet (the existing honey drop + a procedural Royal Jelly
    /// comb cell), wraps it in a <see cref="TMP_SpriteAsset"/> with named
    /// characters <c>honey</c>/<c>jelly</c>, and registers it as the TMP
    /// default sprite asset — so any UI text can show a currency as its image
    /// via <c>&lt;sprite name="honey"&gt;</c>. Additive and idempotent: the PNG
    /// is only written when missing (so future final art survives re-runs) and
    /// the sprite asset tables are re-authored in place.
    /// </summary>
    public static class CurrencyGlyphsBuilder
    {
        private const string AtlasPath = "Assets/Sprites/CurrencyGlyphs.png";
        private const string HoneySourcePath = "Assets/Sprites/HoneyDrop.png";
        private const string SpriteAssetFolder = "Assets/Data/UI";
        private const string SpriteAssetPath = SpriteAssetFolder + "/CurrencyGlyphs.asset";
        private const string TmpSettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

        private const int CellSize = 32;

        // Royal Jelly palette (ASSET_GENERATION §2.8): pearly cream fill in a
        // gold-rimmed comb cell.
        private static readonly Color32 JellyCream = new Color32(245, 237, 205, 255);
        private static readonly Color32 JellyShade = new Color32(226, 213, 168, 255);
        private static readonly Color32 JellyGlint = new Color32(255, 253, 244, 255);
        private static readonly Color32 RimGold = new Color32(255, 195, 11, 255);
        private static readonly Color32 RimBrown = new Color32(140, 90, 43, 255);

        [MenuItem("SurveHive/Build Currency Glyphs")]
        public static void Apply()
        {
            EnsureAtlasPng();
            AssetDatabase.Refresh();
            ConfigureAtlasImporter();

            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            TMP_SpriteAsset spriteAsset = BuildSpriteAsset(texture);
            AssignAsTmpDefault(spriteAsset);

            AssetDatabase.SaveAssets();
            Debug.Log("SurveHive currency glyphs build complete.");
        }

        // ------------------------------------------------------------------
        // 1) The 64×32 sheet: honey drop (4× nearest-neighbor) | jelly cell.
        //    Written only when missing so swapped-in final art survives.
        // ------------------------------------------------------------------
        private static void EnsureAtlasPng()
        {
            if (File.Exists(AtlasPath))
            {
                return;
            }

            var atlas = new Texture2D(CellSize * 2, CellSize, TextureFormat.RGBA32, false);
            var clear = new Color32[atlas.width * atlas.height];
            atlas.SetPixels32(clear);

            DrawHoneyDrop(atlas);
            DrawJellyCell(atlas, CellSize);

            File.WriteAllBytes(AtlasPath, atlas.EncodeToPNG());
            Object.DestroyImmediate(atlas);
        }

        // Upscales the shipped 7×8 honey-drop pickup sprite 4× (28×32) into the
        // left cell, reading the PNG bytes directly so importer readability
        // settings don't matter.
        private static void DrawHoneyDrop(Texture2D atlas)
        {
            var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            source.LoadImage(File.ReadAllBytes(HoneySourcePath));

            const int scale = 4;
            int offsetX = (CellSize - source.width * scale) / 2;
            int offsetY = (CellSize - source.height * scale) / 2;

            for (int y = 0; y < source.height; y++)
            {
                for (int x = 0; x < source.width; x++)
                {
                    Color32 pixel = source.GetPixel(x, y);
                    for (int dy = 0; dy < scale; dy++)
                    {
                        for (int dx = 0; dx < scale; dx++)
                        {
                            atlas.SetPixel(offsetX + x * scale + dx, offsetY + y * scale + dy, pixel);
                        }
                    }
                }
            }

            Object.DestroyImmediate(source);
        }

        // A pointy-top hexagonal comb cell: gold rim over a brown edge, pearly
        // cream fill with a lower-right shade and an upper-left glint.
        private static void DrawJellyCell(Texture2D atlas, int cellOffsetX)
        {
            const float radius = 14f;
            Vector2 center = new Vector2(15.5f, 15.5f);

            for (int y = 0; y < CellSize; y++)
            {
                for (int x = 0; x < CellSize; x++)
                {
                    float dx = Mathf.Abs(x - center.x);
                    float dy = Mathf.Abs(y - center.y);
                    // Pointy-top hex "distance": max of the vertical extent and
                    // the two slanted edges.
                    float hexDistance = Mathf.Max(dy, dy * 0.5f + dx * 0.866f);

                    if (hexDistance > radius)
                    {
                        continue;
                    }

                    Color32 color;
                    if (hexDistance > radius - 2f)
                    {
                        color = RimBrown;
                    }
                    else if (hexDistance > radius - 4f)
                    {
                        color = RimGold;
                    }
                    else if (x - center.x < -2f && y - center.y > 4f && hexDistance < radius - 5f)
                    {
                        color = JellyGlint;
                    }
                    else if (x - center.x > 3f && y - center.y < -3f)
                    {
                        color = JellyShade;
                    }
                    else
                    {
                        color = JellyCream;
                    }

                    atlas.SetPixel(cellOffsetX + x, y, color);
                }
            }
        }

        private static void ConfigureAtlasImporter()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------
        // 2) The TMP sprite asset: two named glyphs over the sheet. Tables are
        //    re-authored every run (find-or-create keeps the asset GUID).
        // ------------------------------------------------------------------
        private static TMP_SpriteAsset BuildSpriteAsset(Texture2D texture)
        {
            if (!AssetDatabase.IsValidFolder(SpriteAssetFolder))
            {
                AssetDatabase.CreateFolder("Assets/Data", "UI");
            }

            var asset = AssetDatabase.LoadAssetAtPath<TMP_SpriteAsset>(SpriteAssetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();
                AssetDatabase.CreateAsset(asset, SpriteAssetPath);
            }

            asset.spriteSheet = texture;
            asset.hashCode = TMP_TextUtilities.GetSimpleHashCode(Path.GetFileNameWithoutExtension(SpriteAssetPath));

            asset.spriteGlyphTable.Clear();
            asset.spriteCharacterTable.Clear();
            AddGlyph(asset, 0, 0, "honey", 0xE000);
            AddGlyph(asset, 1, CellSize, "jelly", 0xE001);
            asset.UpdateLookupTables();

            // Sizing: TMP scales sprites by fontSize / faceInfo.pointSize, so a
            // 32pt face makes each glyph render at roughly the surrounding text
            // height.
            var so = new SerializedObject(asset);
            // Without a version stamp TMP treats the asset as legacy and wipes
            // the glyph/character tables on first use (UpgradeSpriteAsset);
            // the setter is internal, so stamp the serialized field directly.
            so.FindProperty("m_Version").stringValue = "1.1.0";
            so.FindProperty("m_FaceInfo.m_PointSize").floatValue = CellSize;
            so.FindProperty("m_FaceInfo.m_Scale").floatValue = 1f;
            so.FindProperty("m_FaceInfo.m_AscentLine").floatValue = 26f;
            so.FindProperty("m_FaceInfo.m_LineHeight").floatValue = CellSize;
            so.ApplyModifiedPropertiesWithoutUndo();

            EnsureSpriteMaterial(asset, texture);
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void AddGlyph(TMP_SpriteAsset asset, uint index, int rectX, string name, uint unicode)
        {
            var glyph = new TMP_SpriteGlyph
            {
                index = index,
                glyphRect = new GlyphRect(rectX, 0, CellSize, CellSize),
                // Bearing lifts the sprite so it sits on the text baseline with
                // a small dip below (reads centered against digits).
                metrics = new GlyphMetrics(CellSize, CellSize, 0f, CellSize - 5f, CellSize + 2f),
                scale = 1f,
            };
            asset.spriteGlyphTable.Add(glyph);

            var character = new TMP_SpriteCharacter(unicode, glyph)
            {
                name = name,
                scale = 1f,
            };
            asset.spriteCharacterTable.Add(character);
        }

        private static void EnsureSpriteMaterial(TMP_SpriteAsset asset, Texture2D texture)
        {
            if (asset.material == null)
            {
                var material = new Material(Shader.Find("TextMeshPro/Sprite"))
                {
                    name = "CurrencyGlyphs Material",
                };
                AssetDatabase.AddObjectToAsset(material, asset);
                asset.material = material;
            }

            asset.material.SetTexture(ShaderUtilities.ID_MainTex, texture);
        }

        // ------------------------------------------------------------------
        // 3) Register as the TMP default sprite asset so <sprite name="...">
        //    resolves in every text without per-component wiring.
        // ------------------------------------------------------------------
        private static void AssignAsTmpDefault(TMP_SpriteAsset asset)
        {
            var settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (settings == null)
            {
                Debug.LogError($"CurrencyGlyphsBuilder: TMP Settings not found at {TmpSettingsPath}.");
                return;
            }

            var so = new SerializedObject(settings);
            so.FindProperty("m_defaultSpriteAsset").objectReferenceValue = asset;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }
    }
}

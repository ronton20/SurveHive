using System.IO;
using SurveHive.View;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace SurveHive.BuildTools
{
    /// <summary>
    /// PLAN 6B — a real honeycomb floor for the Beehive world, replacing the
    /// void the arena rendered as. Additive and idempotent; three passes:
    ///   1. A seamless honeycomb tile PNG (procedural Voronoi comb — warm
    ///      comb-brown cells with wax-cream centres and dark cell walls),
    ///      written only when missing so final art (ASSET_GENERATION §1.3) can
    ///      overwrite it without a builder edit.
    ///   2. A <see cref="Tile"/> asset pointing at that sprite.
    ///   3. The Beehive Grid + Tilemap (sorting order well below all gameplay),
    ///      filled over a generous region and wired to follow the run camera via
    ///      <see cref="InfiniteTileFloor"/> so a modest fill reads as an endless,
    ///      world-locked floor under an unbounded run.
    /// Rebuilds only its own generated nodes (the HiveFloorGrid), never the
    /// hand-tuned scene around it.
    /// </summary>
    public static class HiveFloorBuilder
    {
        private const string TilesFolder = "Assets/Sprites/Tiles";
        private const string TexturePath = TilesFolder + "/HiveFloor.png";
        private const string TileAssetPath = TilesFolder + "/HiveFloorTile.asset";
        private const string BeehiveScenePath = "Assets/Scenes/Beehive.unity";

        private const string GridName = "HiveFloorGrid";
        private const string TilemapName = "Tilemap";

        // 432px tile @ PPU 48 = a 9-world-unit tile: one big honeycomb block.
        // A HexColumns×~4 lattice makes each cell ~3 units across (vs the ~1-unit
        // hero), so the floor reads as a calm large-scale comb instead of a busy
        // grid of tiny cells. Blocks are seamless, so they join across the fill.
        private const int TextureSize = 432;
        private const float PixelsPerUnit = 48f;
        private const float TileWorldSize = TextureSize / PixelsPerUnit; // 9.0
        // Hex columns across one 9-unit block (rows derived to keep cells regular).
        private const int HexColumns = 3;
        // Draw well beneath every gameplay sprite (zones sit at -1).
        private const int FloorSortingOrder = -100;
        // Half-extent of the filled region, in cells. With 9-unit cells, ±3 cells
        // (±27 units) covers the ~21×12-unit view plus a tile of margin;
        // InfiniteTileFloor keeps it centred, so a small fill reads as endless.
        private const int FillHalfExtent = 3;

        // Soft warm hive palette — low contrast so it reads calm, not noisy.
        private static readonly Color32 WallColor = new Color32(150, 106, 54, 255);   // soft comb seam
        private static readonly Color32 CellCenter = new Color32(226, 182, 104, 255); // waxy centre
        private static readonly Color32 CellEdge = new Color32(184, 138, 72, 255);    // gentle comb-brown rim

        // Scattered honey: soft, semi-transparent puddles in varied warm tones baked
        // into the tile. Positions are tile-space pixels (0..TextureSize) and wrap
        // toroidally, so the scatter tiles seamlessly with the comb underneath.
        private static readonly Color32 Amber = new Color32(220, 148, 40, 255);
        private static readonly Color32 Gold = new Color32(236, 182, 72, 255);
        private static readonly Color32 Nectar = new Color32(248, 216, 134, 255);
        private static readonly Color32 Caramel = new Color32(158, 100, 34, 255);
        private static readonly Color32 HoneyOrange = new Color32(226, 156, 52, 255);

        private readonly struct HoneySplat
        {
            public readonly float X, Y, Radius, Alpha;
            public readonly Color32 Color;

            public HoneySplat(float x, float y, float radius, Color32 color, float alpha)
            {
                X = x; Y = y; Radius = radius; Color = color; Alpha = alpha;
            }
        }

        private static readonly HoneySplat[] HoneySplats =
        {
            new HoneySplat(70, 92, 42, Amber, 0.52f),
            new HoneySplat(300, 58, 30, Gold, 0.48f),
            new HoneySplat(182, 205, 48, Caramel, 0.42f),
            new HoneySplat(382, 262, 36, Nectar, 0.50f),
            new HoneySplat(112, 330, 28, HoneyOrange, 0.46f),
            new HoneySplat(262, 382, 44, Amber, 0.50f),
            new HoneySplat(44, 232, 24, Nectar, 0.44f),
            new HoneySplat(348, 150, 22, Caramel, 0.40f),
            new HoneySplat(200, 110, 18, Gold, 0.40f),
            new HoneySplat(150, 40, 16, HoneyOrange, 0.38f),
        };

        // Supersample factor: render the comb at this multiple then box-downsample,
        // so cell seams are anti-aliased and easy on the eyes (no jagged dark pixels).
        private const int Supersample = 6;

        [MenuItem("SurveHive/Phase 6B — Hive Floor Tileset")]
        public static void Apply()
        {
            EnsureTileTexture();
            Tile tile = EnsureTileAsset();
            if (tile == null)
            {
                Debug.LogError("[HiveFloorBuilder] Could not create the hive floor tile.");
                return;
            }

            BuildSceneTilemap(tile);
            Debug.Log("[HiveFloorBuilder] Honeycomb floor tile + Beehive tilemap built.");
        }

        // ------------------------------------------------------------------
        // 1) Procedural seamless honeycomb PNG (only-if-missing).
        // ------------------------------------------------------------------
        private static void EnsureTileTexture()
        {
            if (!AssetDatabase.IsValidFolder(TilesFolder))
            {
                AssetDatabase.CreateFolder("Assets/Sprites", "Tiles");
            }

            if (!File.Exists(TexturePath))
            {
                var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, false);
                texture.SetPixels32(GenerateHoneycomb(TextureSize));
                File.WriteAllBytes(TexturePath, texture.EncodeToPNG());
                Object.DestroyImmediate(texture);
                AssetDatabase.Refresh();
            }

            ImportTileTexture();
        }

        /// <summary>
        /// Voronoi honeycomb over a toroidal (wrapping) triangular lattice: the
        /// Voronoi cells of such a lattice are hexagons, and because the lattice
        /// is periodic in the texture the pattern tiles seamlessly. A pixel near
        /// the boundary between its two nearest centres is a soft cell seam;
        /// interior pixels grade from a waxy centre out to a comb-brown rim.
        ///
        /// Rendered at <see cref="Supersample"/>× and box-downsampled so seams are
        /// anti-aliased into gentle warm lines — smooth and easy on the eyes,
        /// rather than the jagged near-black grid a hard 1-px threshold produced.
        /// Finally, soft honey puddles (<see cref="HoneySplats"/>) are baked over the
        /// comb in varied warm tones, wrapped toroidally so they stay seamless.
        /// </summary>
        private static Color32[] GenerateHoneycomb(int size)
        {
            int ss = size * Supersample;

            // Offset-row lattice, period = texture size in both axes so it wraps.
            // HexColumns cells span the block; dx sets their width and dy = 0.75·dx
            // keeps the 2 horizontal + 4 diagonal neighbours equidistant, so each
            // Voronoi cell is a regular hexagon (not a diamond). Rows are derived
            // and forced even so the half-row offset repeats cleanly across the seam.
            int dx = ss / HexColumns;
            int rows = Mathf.Max(2, Mathf.RoundToInt(ss / (dx * 0.75f)));
            if ((rows & 1) == 1)
            {
                rows++;
            }

            float dyF = (float)ss / rows;
            int cols = HexColumns;

            int centerCount = cols * rows;
            var centers = new Vector2[centerCount];
            int c = 0;
            for (int j = 0; j < rows; j++)
            {
                float rowOffset = (j & 1) == 1 ? dx * 0.5f : 0f;
                for (int i = 0; i < cols; i++)
                {
                    centers[c++] = new Vector2((i * dx + rowOffset) % ss, (j * dyF) % ss);
                }
            }

            // A cell's rough radius sets the interior gradient falloff. The seam
            // fades from full wall (gap ≤ wallInner) to none (gap ≥ wallOuter),
            // capped low so it reads as a soft warm line, never a hard edge.
            float cellRadius = dyF;
            float wallInner = 2.5f * Supersample;
            float wallOuter = 8f * Supersample;
            const float wallStrength = 0.5f;

            // Accumulate the supersampled comb straight into downsampled buckets so
            // we never hold the full ss×ss image — memory stays O(size²) regardless
            // of the supersample factor.
            var sum = new Vector3[size * size];
            float inv = 1f / (Supersample * Supersample);

            for (int y = 0; y < ss; y++)
            {
                int by = (y / Supersample) * size;
                for (int x = 0; x < ss; x++)
                {
                    float best = float.MaxValue;
                    float second = float.MaxValue;
                    for (int k = 0; k < centerCount; k++)
                    {
                        float d = ToroidalSqrDistance(x, y, centers[k], ss);
                        if (d < best)
                        {
                            second = best;
                            best = d;
                        }
                        else if (d < second)
                        {
                            second = d;
                        }
                    }

                    float nearest = Mathf.Sqrt(best);
                    float gap = Mathf.Sqrt(second) - nearest;

                    // Ease off the darkest rim (×0.85) so cells stay soft, not busy.
                    float t = Mathf.Clamp01(nearest / cellRadius) * 0.85f;
                    Color32 interior = Color32.Lerp(CellCenter, CellEdge, t);
                    float wall = (1f - Smoothstep(wallInner, wallOuter, gap)) * wallStrength;

                    float r = interior.r * (1f - wall) + WallColor.r * wall;
                    float g = interior.g * (1f - wall) + WallColor.g * wall;
                    float b = interior.b * (1f - wall) + WallColor.b * wall;

                    // Bake scattered honey over the comb: each splat is a soft puddle
                    // with a subtle offset highlight, wrapped toroidally to stay seamless.
                    for (int s = 0; s < HoneySplats.Length; s++)
                    {
                        HoneySplat splat = HoneySplats[s];
                        float cx = splat.X * Supersample;
                        float cy = splat.Y * Supersample;
                        float rad = splat.Radius * Supersample;

                        float pd = ToroidalDistance(x, y, cx, cy, ss);
                        float a = splat.Alpha * Smoothstep(rad, rad * 0.25f, pd);
                        r = r * (1f - a) + splat.Color.r * a;
                        g = g * (1f - a) + splat.Color.g * a;
                        b = b * (1f - a) + splat.Color.b * a;

                        float hd = ToroidalDistance(x, y, cx - rad * 0.22f, cy - rad * 0.22f, ss);
                        float ha = 0.22f * Smoothstep(rad * 0.55f, rad * 0.1f, hd);
                        r = r * (1f - ha) + Mathf.Min(splat.Color.r + 52, 255) * ha;
                        g = g * (1f - ha) + Mathf.Min(splat.Color.g + 52, 255) * ha;
                        b = b * (1f - ha) + Mathf.Min(splat.Color.b + 52, 255) * ha;
                    }

                    sum[by + (x / Supersample)] += new Vector3(r, g, b);
                }
            }

            var pixels = new Color32[size * size];
            for (int i = 0; i < pixels.Length; i++)
            {
                Vector3 avg = sum[i] * inv;
                pixels[i] = new Color32(
                    (byte)Mathf.Clamp(Mathf.RoundToInt(avg.x), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(avg.y), 0, 255),
                    (byte)Mathf.Clamp(Mathf.RoundToInt(avg.z), 0, 255),
                    255);
            }

            return pixels;
        }

        private static float Smoothstep(float edge0, float edge1, float x)
        {
            float t = Mathf.Clamp01((x - edge0) / (edge1 - edge0));
            return t * t * (3f - 2f * t);
        }

        // Squared distance to a centre on a wrapping torus (shortest across the
        // seam), so the pattern is continuous across tile boundaries.
        private static float ToroidalSqrDistance(int x, int y, Vector2 center, int size)
        {
            float dx = Mathf.Abs(x - center.x);
            float dy = Mathf.Abs(y - center.y);
            if (dx > size * 0.5f)
            {
                dx = size - dx;
            }

            if (dy > size * 0.5f)
            {
                dy = size - dy;
            }

            return dx * dx + dy * dy;
        }

        // Distance to a point on a wrapping torus, for seamless honey splats.
        private static float ToroidalDistance(float x, float y, float cx, float cy, int size)
        {
            float dx = Mathf.Abs(x - cx);
            float dy = Mathf.Abs(y - cy);
            if (dx > size * 0.5f)
            {
                dx = size - dx;
            }

            if (dy > size * 0.5f)
            {
                dy = size - dy;
            }

            return Mathf.Sqrt(dx * dx + dy * dy);
        }

        private static void ImportTileTexture()
        {
            AssetDatabase.ImportAsset(TexturePath, ImportAssetOptions.ForceUpdate);
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            if (importer == null)
            {
                Debug.LogError($"[HiveFloorBuilder] Missing tile texture {TexturePath}.");
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritePixelsPerUnit = PixelsPerUnit;
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.alphaIsTransparency = false;
            importer.SaveAndReimport();
        }

        // ------------------------------------------------------------------
        // 2) The Tile asset.
        // ------------------------------------------------------------------
        private static Tile EnsureTileAsset()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);
            if (sprite == null)
            {
                Debug.LogError($"[HiveFloorBuilder] Tile sprite not imported at {TexturePath}.");
                return null;
            }

            var tile = AssetDatabase.LoadAssetAtPath<Tile>(TileAssetPath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, TileAssetPath);
            }

            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            AssetDatabase.SaveAssets();
            return tile;
        }

        // ------------------------------------------------------------------
        // 3) Beehive Grid + Tilemap, filled and wired to the camera.
        // ------------------------------------------------------------------
        private static void BuildSceneTilemap(Tile tile)
        {
            EditorSceneManager.OpenScene(BeehiveScenePath, OpenSceneMode.Single);

            // Rebuild our own node from scratch each run (idempotent).
            GameObject existing = GameObject.Find(GridName);
            if (existing != null)
            {
                Object.DestroyImmediate(existing);
            }

            var gridGo = new GameObject(GridName, typeof(Grid));
            gridGo.transform.position = Vector3.zero;
            var grid = gridGo.GetComponent<Grid>();
            grid.cellSize = new Vector3(TileWorldSize, TileWorldSize, 0f);

            var mapGo = new GameObject(TilemapName, typeof(Tilemap), typeof(TilemapRenderer));
            mapGo.transform.SetParent(gridGo.transform, false);
            var tilemap = mapGo.GetComponent<Tilemap>();
            var renderer = mapGo.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = FloorSortingOrder;

            // Fill the region in one batched call.
            int span = FillHalfExtent * 2 + 1;
            var bounds = new BoundsInt(-FillHalfExtent, -FillHalfExtent, 0, span, span, 1);
            var tiles = new TileBase[span * span];
            for (int i = 0; i < tiles.Length; i++)
            {
                tiles[i] = tile;
            }

            tilemap.SetTilesBlock(bounds, tiles);

            // Follow the run camera so the floor is effectively endless.
            var floor = gridGo.AddComponent<InfiniteTileFloor>();
            GameObject camGo = GameObject.FindWithTag("MainCamera");
            var floorSo = new SerializedObject(floor);
            floorSo.FindProperty("_target").objectReferenceValue = camGo != null ? camGo.transform : null;
            floorSo.FindProperty("_period").floatValue = TileWorldSize;
            floorSo.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(floor);

            if (camGo == null)
            {
                Debug.LogWarning("[HiveFloorBuilder] Main Camera not found — floor follow left unwired.");
            }

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

public class UiAssetLibrary
{
    public const string SPRITES_FOLDER = "Assets/UI/Sprites";
    public const string FONTS_FOLDER = "Assets/UI/Fonts";
    public const string FONT_ASSETS_FOLDER = "Assets/UI/Fonts/TMP";

    private const int SAMPLING_POINT_SIZE = 90;
    private const int ATLAS_PADDING = 9;
    private const int ATLAS_SIZE = 1024;
    private const int SPRITE_MAX_SIZE = 512;
    private const string PRECACHED_CHARACTERS =
        " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
        "ąćęłńóśźżĄĆĘŁŃÓŚŹŻ×·–—…";

    private static readonly Dictionary<string, float> SpriteBorders = new Dictionary<string, float>
    {
        { "Rect_R6", 12f },
        { "Rect_R10", 16f },
        { "Outline_R6", 12f },
        { "Outline_R10", 16f },
    };

    private static readonly HashSet<string> TileableSprites = new HashSet<string> { "Wave_Band" };

    public TMP_FontAsset Display { get; private set; }
    public TMP_FontAsset Body { get; private set; }
    public TMP_FontAsset BodyMedium { get; private set; }
    public TMP_FontAsset BodyBold => Display;

    public const string LOGO_SOURCE_PATH = "Assets/UI/Logo/ClubLogo.png";
    public const string LOGO_SPRITE_NAME = "Logo_Club";

    public static UiAssetLibrary Load()
    {
        AssetDatabase.Refresh();
        GenerateLogoSprite();
        ConfigureSpriteImporters();

        var library = new UiAssetLibrary
        {
            Display = EnsureFontAsset("Poppins-Bold", "Poppins Bold SDF"),
            Body = EnsureFontAsset("Poppins-SemiBold", "Poppins SemiBold SDF"),
            BodyMedium = EnsureFontAsset("Poppins-Medium", "Poppins Medium SDF"),
        };

        AssetDatabase.SaveAssets();
        return library;
    }

    public Sprite Sprite(string name)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_FOLDER}/{name}.png");
        if (sprite == null) throw new FileNotFoundException($"UI sprite '{name}' not found in {SPRITES_FOLDER}");
        return sprite;
    }

    public Sprite OptionalSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{SPRITES_FOLDER}/{name}.png");
    }

    private static void GenerateLogoSprite()
    {
        if (!File.Exists(LOGO_SOURCE_PATH)) return;

        var source = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            if (!source.LoadImage(File.ReadAllBytes(LOGO_SOURCE_PATH)))
            {
                Debug.LogWarning($"{nameof(UiAssetLibrary)}: could not decode {LOGO_SOURCE_PATH}");
                return;
            }

            Color32[] pixels = source.GetPixels32();
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 pixel = pixels[i];
                float luminance = (0.299f * pixel.r + 0.587f * pixel.g + 0.114f * pixel.b) / 255f;
                byte alpha = (byte)Mathf.RoundToInt(pixel.a * (1f - luminance));
                pixels[i] = new Color32(255, 255, 255, alpha);
            }

            source.SetPixels32(pixels);
            source.Apply();

            string targetPath = $"{SPRITES_FOLDER}/{LOGO_SPRITE_NAME}.png";
            File.WriteAllBytes(targetPath, source.EncodeToPNG());
            AssetDatabase.ImportAsset(targetPath, ImportAssetOptions.ForceUpdate);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(source);
        }
    }

    private static void ConfigureSpriteImporters()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { SPRITES_FOLDER }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;

            string name = Path.GetFileNameWithoutExtension(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 100f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.sRGBTexture = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TileableSprites.Contains(name) ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = SPRITE_MAX_SIZE;
            importer.spriteBorder = SpriteBorders.TryGetValue(name, out float border)
                ? new Vector4(border, border, border, border)
                : Vector4.zero;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
    }

    private static TMP_FontAsset EnsureFontAsset(string fontFileName, string assetName)
    {
        EnsureFolder(FONT_ASSETS_FOLDER);
        string assetPath = $"{FONT_ASSETS_FOLDER}/{assetName}.asset";

        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null)
        {
            PrecacheGlyphs(existing);
            return existing;
        }

        string fontPath = $"{FONTS_FOLDER}/{fontFileName}.ttf";
        var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null) throw new FileNotFoundException($"Font file '{fontPath}' not found");

        TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
            font, SAMPLING_POINT_SIZE, ATLAS_PADDING, GlyphRenderMode.SDFAA, ATLAS_SIZE, ATLAS_SIZE,
            AtlasPopulationMode.Dynamic, true);
        if (fontAsset == null) throw new InvalidOperationException($"Could not create TMP font asset from '{fontPath}'");

        fontAsset.name = assetName;
        AssetDatabase.CreateAsset(fontAsset, assetPath);

        Texture2D atlas = fontAsset.atlasTextures[0];
        atlas.name = assetName + " Atlas";
        AssetDatabase.AddObjectToAsset(atlas, fontAsset);

        Material material = fontAsset.material;
        material.name = assetName + " Material";
        AssetDatabase.AddObjectToAsset(material, fontAsset);

        PrecacheGlyphs(fontAsset);
        AssetDatabase.SaveAssets();
        return fontAsset;
    }

    private static void PrecacheGlyphs(TMP_FontAsset fontAsset)
    {
        var so = new SerializedObject(fontAsset);
        SerializedProperty clearOnBuild = so.FindProperty("m_ClearDynamicDataOnBuild");
        if (clearOnBuild != null && clearOnBuild.boolValue)
        {
            clearOnBuild.boolValue = false;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        if (fontAsset.characterTable != null && fontAsset.characterTable.Count > 0) return;

        fontAsset.TryAddCharacters(PRECACHED_CHARACTERS, out string missing);
        if (!string.IsNullOrEmpty(missing))
        {
            Debug.Log($"{fontAsset.name}: glyphs missing for '{missing}', fallback font will be used");
        }

        EditorUtility.SetDirty(fontAsset);
        if (fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0) EditorUtility.SetDirty(fontAsset.atlasTextures[0]);
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        if (!string.IsNullOrEmpty(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }
}

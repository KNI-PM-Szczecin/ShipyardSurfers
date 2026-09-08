using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Unity.InferenceEngine;

public static class PoseModelLoader
{
    private const string DESCRIPTION_FIELD = "modelAssetData";
    private const string WEIGHTS_FIELD = "modelWeightsChunks";
    private const string VALUE_FIELD = "value";
    private const BindingFlags FIELD_FLAGS = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static Task<Model> LoadAsync(ModelAsset asset)
    {
        if (asset == null) return Task.FromException<Model>(new ArgumentNullException(nameof(asset)));

        if (!TryReadChunks(asset, out byte[][] chunks))
        {
            return Task.FromException<Model>(new InvalidOperationException("Model asset layout is not readable, falling back to synchronous load"));
        }

        return Task.Run(() =>
        {
            using MemoryStream stream = Concatenate(chunks);
            return ModelLoader.Load(stream);
        });
    }

    private static bool TryReadChunks(ModelAsset asset, out byte[][] chunks)
    {
        chunks = null;

        FieldInfo descriptionField = typeof(ModelAsset).GetField(DESCRIPTION_FIELD, FIELD_FLAGS);
        FieldInfo weightsField = typeof(ModelAsset).GetField(WEIGHTS_FIELD, FIELD_FLAGS);
        if (descriptionField == null || weightsField == null) return false;

        byte[] description = ValueOf(descriptionField.GetValue(asset));
        if (description == null) return false;

        var weights = weightsField.GetValue(asset) as Array;
        int weightCount = weights?.Length ?? 0;

        chunks = new byte[weightCount + 1][];
        chunks[0] = description;
        for (int i = 0; i < weightCount; i++)
        {
            byte[] chunk = ValueOf(weights.GetValue(i));
            if (chunk == null) return false;
            chunks[i + 1] = chunk;
        }

        return true;
    }

    private static byte[] ValueOf(object container)
    {
        if (container == null) return null;
        return container.GetType().GetField(VALUE_FIELD, FIELD_FLAGS)?.GetValue(container) as byte[];
    }

    private static MemoryStream Concatenate(byte[][] chunks)
    {
        long total = 0;
        foreach (byte[] chunk in chunks) total += chunk.Length;

        var stream = new MemoryStream((int)total);
        foreach (byte[] chunk in chunks) stream.Write(chunk, 0, chunk.Length);
        stream.Position = 0;
        return stream;
    }
}

using AssetsTools.NET;
using AssetsTools.NET.Extra;
using System;
using System.IO;

namespace UABEANext4.AssetWorkspace;

public partial class Workspace
{
    public void CompressBundleToFile(
        WorkspaceItem bundleItem,
        string outputPath,
        AssetBundleCompressionType compressionType,
        IAssetBundleCompressProgress? progress = null)
    {
        if (bundleItem.ObjectType != WorkspaceItemType.BundleFile || bundleItem.Object is not BundleFileInstance)
        {
            throw new InvalidOperationException("Selected workspace item is not a bundle.");
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new InvalidOperationException("Output path is empty.");
        }

        string? outputDirectory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        // Используем временный файл вместо MemoryStream,
        // чтобы поддерживать бандлы размером более 2 ГБ
        string tempPath = Path.Combine(
            outputDirectory ?? Path.GetTempPath(),
            "~compress_temp_" + Path.GetRandomFileName());

        try
        {
            using (FileStream uncompressedBundleStream = new(
                tempPath, FileMode.Create, FileAccess.ReadWrite,
                FileShare.None, 81920, FileOptions.DeleteOnClose))
            {
                WriteBundleFile(bundleItem, uncompressedBundleStream);
                uncompressedBundleStream.Position = 0;

                AssetBundleFile bundleToPack = new();
                bundleToPack.Read(new AssetsFileReader(uncompressedBundleStream));

                using FileStream fs = new(outputPath, FileMode.Create, FileAccess.Write, FileShare.Read);
                using AssetsFileWriter writer = new(fs);
                bundleToPack.Pack(writer, compressionType, true, progress);
            }
        }
        finally
        {
            // На случай, если FileOptions.DeleteOnClose не сработал
            try
            {
                if (File.Exists(tempPath))
                    File.Delete(tempPath);
            }
            catch
            {
                // Игнорируем — файл уже удалён или будет удалён ОС
            }
        }
    }
}

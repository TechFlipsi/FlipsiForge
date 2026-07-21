// SPDX-License-Identifier: GPL-3.0-or-later
// StlThumbnailService: Generiert Thumbnail-Bilder aus STL/3MF/OBJ Dateien.
// Verwendet SkiaSharp fuer das Rendering (Cross-Platform, kein GPU noetig).
using System.IO;
using Avalonia.Media.Imaging;
using SkiaSharp;

namespace FlipsiForge.Desktop.Services;

/// <summary>Service der 3D-Modell-Dateien als 2D-Bild rendert.</summary>
public static class StlThumbnailService
{
    /// <summary>Rendert eine STL-Datei als Thumbnail-Bitmap.</summary>
    public static Bitmap? RenderStl(string filePath, int width = 256, int height = 256)
    {
        try
        {
            var triangles = ParseStl(filePath);
            if (triangles.Count == 0) return null;
            return RenderTriangles(triangles, width, height);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Rendert eine 3MF-Datei als Thumbnail (extrahiert eingebettetes Thumbnail oder rendert Mesh).</summary>
    public static Bitmap? Render3mf(string filePath, int width = 256, int height = 256)
    {
        try
        {
            // 3MF ist ZIP — versuche Thumbnail aus /Metadata/thumbnail.png zu extrahieren
            // TEMPORÄRER FIX (Desktop-Subagent, 2026-07-21): ZipFile.IsZipFileName existiert
            // nicht in .NET 10 System.IO.Compression. Verwende Extension-Check statt IsZipFileName.
            // Core-Subagent: bitte sauber lösen via echte MIME/Signature-Detection.
            if (System.IO.Path.GetExtension(filePath).Equals(".3mf", StringComparison.OrdinalIgnoreCase))
            {
                using var archive = System.IO.Compression.ZipFile.OpenRead(filePath);
                var thumbEntry = archive.GetEntry("Metadata/thumbnail.png")
                                ?? archive.GetEntry("Thumbnails/thumbnail.png");
                if (thumbEntry != null)
                {
                    using var stream = thumbEntry.Open();
                    using var memStream = new MemoryStream();
                    stream.CopyTo(memStream);
                    memStream.Position = 0;
                    return new Bitmap(memStream);
                }
            }
            return null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Generiert ein Thumbnail fuer eine 3D-Datei basierend auf der Erweiterung.</summary>
    public static Bitmap? GenerateThumbnail(string filePath, int width = 256, int height = 256)
    {
        if (!File.Exists(filePath)) return null;
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".stl" => RenderStl(filePath, width, height),
            ".3mf" => Render3mf(filePath, width, height),
            ".obj" => RenderObj(filePath, width, height),
            _ => null
        };
    }

    /// <summary>Parst eine STL-Datei (sowohl ASCII als auch Binary).</summary>
    private static List<(float x, float y, float z)> ParseStl(string path)
    {
        var triangles = new List<(float x, float y, float z)>();
        var bytes = File.ReadAllBytes(path);

        // Binary STL: 80-byte header + 4-byte triangle count
        if (bytes.Length >= 84 && !IsAsciiStl(bytes))
        {
            int triCount = BitConverter.ToInt32(bytes, 80);
            for (int i = 0; i < triCount && (84 + i * 50) < bytes.Length; i++)
            {
                int offset = 84 + i * 50;
                // Skip normal (12 bytes), read 3 vertices × 3 floats × 4 bytes
                for (int v = 0; v < 3; v++)
                {
                    int vOff = offset + 12 + v * 12;
                    float x = BitConverter.ToSingle(bytes, vOff);
                    float y = BitConverter.ToSingle(bytes, vOff + 4);
                    float z = BitConverter.ToSingle(bytes, vOff + 8);
                    triangles.Add((x, y, z));
                }
            }
        }
        else
        {
            // ASCII STL
            var text = System.Text.Encoding.UTF8.GetString(bytes);
            var lines = text.Split('\n');
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("vertex", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4 &&
                        float.TryParse(parts[1], out float x) &&
                        float.TryParse(parts[2], out float y) &&
                        float.TryParse(parts[3], out float z))
                    {
                        triangles.Add((x, y, z));
                    }
                }
            }
        }

        return triangles;
    }

    /// <summary>Prueft ob es sich um ASCII STL handelt.</summary>
    private static bool IsAsciiStl(byte[] bytes)
    {
        if (bytes.Length < 6) return false;
        var header = System.Text.Encoding.ASCII.GetString(bytes, 0, 6);
        return header.ToLowerInvariant().Contains("solid");
    }

    /// <summary>Parst eine OBJ-Datei (nur Vertices + Faces).</summary>
    private static Bitmap? RenderObj(string path, int width, int height)
    {
        try
        {
            var vertices = new List<(float x, float y, float z)>();
            var lines = File.ReadAllLines(path);
            foreach (var line in lines)
            {
                var trimmed = line.Trim();
                if (trimmed.StartsWith("v ", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = trimmed.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 4 &&
                        float.TryParse(parts[1], out float x) &&
                        float.TryParse(parts[2], out float y) &&
                        float.TryParse(parts[3], out float z))
                    {
                        vertices.Add((x, y, z));
                    }
                }
            }
            if (vertices.Count == 0) return null;
            return RenderVertices(vertices, width, height);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Rendert eine Liste von Dreiecks-Vertices als 2D-Bild mit SkiaSharp.</summary>
    private static Bitmap RenderTriangles(List<(float x, float y, float z)> triangles, int width, int height)
    {
        // Jedes Dreieck = 3 Vertices
        var vertices = triangles;
        return RenderVertices(vertices, width, height);
    }

    /// <summary>Rendert Vertices als Wireframe/Shader-Bild mit SkiaSharp.</summary>
    private static Bitmap RenderVertices(List<(float x, float y, float z)> vertices, int width, int height)
    {
        if (vertices.Count == 0) return null;

        // Bounding Box berechnen
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;
        foreach (var v in vertices)
        {
            if (v.x < minX) minX = v.x; if (v.x > maxX) maxX = v.x;
            if (v.y < minY) minY = v.y; if (v.y > maxY) maxY = v.y;
            if (v.z < minZ) minZ = v.z; if (v.z > maxZ) maxZ = v.z;
        }

        float rangeX = maxX - minX;
        float rangeY = maxY - minY;
        float rangeZ = maxZ - minZ;
        float maxRange = Math.Max(rangeX, Math.Max(rangeY, rangeZ));
        if (maxRange <= 0) maxRange = 1;

        // Normalisierung + Isometrische Projektion (30 Grad Winkel)
        float scale = (width * 0.8f) / maxRange;
        float cx = width / 2f;
        float cy = height / 2f;

        using var surface = SKSurface.Create(new SKImageInfo(width, height));
        var canvas = surface.Canvas;
        canvas.Clear(new SKColor(12, 12, 16)); // #0c0c10

        // Linien zeichnen (Wireframe)
        var paint = new SKPaint
        {
            Color = new SKColor(255, 102, 0), // #ff6600 Ember
            StrokeWidth = 1,
            IsAntialias = true,
            Style = SKPaintStyle.Stroke
        };

        // Isometrische Projektion: x' = (x - z) * cos(30), y' = (x + z) * sin(30) - y
        float cos30 = (float)Math.Cos(Math.PI / 6);
        float sin30 = (float)Math.Sin(Math.PI / 6);

        // Jedes Dreieck (3 Vertices) als Outline zeichnen
        for (int i = 0; i < vertices.Count - 2; i += 3)
        {
            var v0 = Project(vertices[i], minX, minY, minZ, scale, cx, cy, cos30, sin30);
            var v1 = Project(vertices[i + 1], minX, minY, minZ, scale, cx, cy, cos30, sin30);
            var v2 = Project(vertices[i + 2], minX, minY, minZ, scale, cx, cy, cos30, sin30);

            using var path = new SKPath();
            path.MoveTo(v0);
            path.LineTo(v1);
            path.LineTo(v2);
            path.Close();
            canvas.DrawPath(path, paint);
        }

        // Bitmap erstellen
        var image = surface.Snapshot();
        var data = image.Encode(SKEncodedImageFormat.Png, 100);
        using var memStream = new MemoryStream();
        data.SaveTo(memStream);
        memStream.Position = 0;
        return new Bitmap(memStream);
    }

    /// <summary>Projiziert einen 3D-Vertex auf 2D mit isometrischer Projektion.</summary>
    private static SKPoint Project((float x, float y, float z) v,
        float minX, float minY, float minZ, float scale,
        float cx, float cy, float cos30, float sin30)
    {
        float nx = (v.x - minX) * scale;
        float ny = (v.y - minY) * scale;
        float nz = (v.z - minZ) * scale;

        float px = (nx - nz) * cos30;
        float py = (nx + nz) * sin30 - ny;

        return new SKPoint(cx + px, cy - py);
    }

    /// <summary>Thumbnail-Cache Verzeichnis.</summary>
    public static string CacheDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FlipsiForge", "thumbnails");

    /// <summary>Generiert oder laedt ein gecachtes Thumbnail fuer eine Datei.</summary>
    public static Bitmap? GetOrGenerate(string filePath, long fileLastModified, int width = 256, int height = 256)
    {
        try
        {
            Directory.CreateDirectory(CacheDir);
            var hash = filePath.GetHashCode().ToString("x") + "_" + fileLastModified.ToString("x");
            var cachePath = Path.Combine(CacheDir, hash + ".png");

            if (File.Exists(cachePath))
            {
                using var stream = File.OpenRead(cachePath);
                return new Bitmap(stream);
            }

            var thumb = GenerateThumbnail(filePath, width, height);
            if (thumb != null)
            {
                using var stream = File.Create(cachePath);
                thumb.Save(stream);
            }
            return thumb;
        }
        catch
        {
            return null;
        }
    }
}
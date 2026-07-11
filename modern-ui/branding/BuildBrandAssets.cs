using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

internal static class BuildBrandAssets
{
    private static void Main(string[] args)
    {
        if (args.Length != 3) throw new ArgumentException("Usage: BuildBrandAssets <chroma.png> <logo.png> <icon.ico>");
        using (Bitmap source = new Bitmap(args[0]))
        using (Bitmap transparent = RemoveGreenScreen(source))
        using (Bitmap finalLogo = Resize(transparent, 1024))
        {
            finalLogo.Save(args[1], ImageFormat.Png);
            WriteIcon(args[2], finalLogo, new[] { 16, 20, 24, 32, 40, 48, 64, 128, 256 });
        }
    }

    private static Bitmap RemoveGreenScreen(Bitmap source)
    {
        Bitmap result = new Bitmap(source.Width, source.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < source.Height; y++)
        {
            for (int x = 0; x < source.Width; x++)
            {
                Color color = source.GetPixel(x, y);
                double distance = Math.Sqrt(color.R * color.R + (255 - color.G) * (255 - color.G) + color.B * color.B);
                int alpha = distance <= 28 ? 0 : distance >= 150 ? 255 : (int)Math.Round((distance - 28) / 122.0 * 255.0);
                int dominance = color.G - Math.Max(color.R, color.B);
                if (dominance > 15)
                    alpha = Math.Min(alpha, Math.Max(0, 255 - (dominance - 15) * 3));
                int green = color.G;
                if (green > Math.Max(color.R, color.B))
                    green = Math.Max(color.R, color.B);
                result.SetPixel(x, y, Color.FromArgb(alpha, color.R, green, color.B));
            }
        }
        return result;
    }

    private static Bitmap Resize(Bitmap source, int size)
    {
        Bitmap result = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using (Graphics graphics = Graphics.FromImage(result))
        {
            graphics.Clear(Color.Transparent);
            graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingQuality = CompositingQuality.HighQuality;
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
            graphics.DrawImage(source, new Rectangle(0, 0, size, size));
        }
        return result;
    }

    private static byte[] EncodePng(Bitmap source, int size)
    {
        using (Bitmap image = Resize(source, size))
        using (MemoryStream stream = new MemoryStream())
        {
            image.Save(stream, ImageFormat.Png);
            return stream.ToArray();
        }
    }

    private static void WriteIcon(string path, Bitmap source, int[] sizes)
    {
        List<byte[]> images = new List<byte[]>();
        foreach (int size in sizes) images.Add(EncodePng(source, size));

        using (FileStream stream = File.Create(path))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write((ushort)0);
            writer.Write((ushort)1);
            writer.Write((ushort)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                writer.Write((byte)(sizes[i] == 256 ? 0 : sizes[i]));
                writer.Write((byte)0);
                writer.Write((byte)0);
                writer.Write((ushort)1);
                writer.Write((ushort)32);
                writer.Write(images[i].Length);
                writer.Write(offset);
                offset += images[i].Length;
            }
            foreach (byte[] image in images) writer.Write(image);
        }
    }
}

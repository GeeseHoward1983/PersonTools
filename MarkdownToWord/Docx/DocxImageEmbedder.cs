using System.IO;
using System.Text;
using System.Windows.Media.Imaging;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Markdig.Syntax.Inlines;
using A = DocumentFormat.OpenXml.Drawing;
using DW = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;

namespace PersonalTools.MarkdownToWord.Docx
{
    /// <summary>
    /// 嵌入本地图片：解析相对/绝对路径 → 读字节挂 ImagePart → 按像素与 DPI 换算 EMU 插入 Drawing。
    /// http(s)/data/无法读取的图片改为占位文字「[图片未嵌入: …]」（需求 4）。
    /// </summary>
    internal static class DocxImageEmbedder
    {
        private const long EmuPerInch = 914400;
        private const long MaxWidthEmu = 5_486_400; // 约 6 英寸，限制图片不超页面正文宽度

        /// <summary>把图片作为内联 run 嵌入容器；成功返回 true，远程/失败时插占位文字返回 false。</summary>
        public static bool AppendInlineImage(OpenXmlElement parent, LinkInline image, DocxRunStyle style, DocxRenderContext ctx)
        {
            if (!TryResolveLocalImage(image.Url, ctx.BaseDir, out string fullPath, out PartTypeInfo type))
            {
                AppendPlaceholder(parent, image.Url, style);
                return false;
            }

            try
            {
                byte[] bytes = File.ReadAllBytes(fullPath);
                if (!TryGetPixelSize(bytes, out int pixelWidth, out int pixelHeight, out double dpiX, out double dpiY))
                {
                    AppendPlaceholder(parent, image.Url, style);
                    return false;
                }

                ImagePart part = ctx.MainPart.AddImagePart(type);
                using (MemoryStream ms = new(bytes))
                {
                    part.FeedData(ms);
                }

                string relationshipId = ctx.MainPart.GetIdOfPart(part);
                (long cx, long cy) = ComputeEmu(pixelWidth, pixelHeight, dpiX, dpiY);
                Run run = new(BuildDrawing(relationshipId, cx, cy, ctx.NextDrawingId(), Path.GetFileName(fullPath)));
                parent.AppendChild(run);
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException or ArgumentException)
            {
                AppendPlaceholder(parent, image.Url, style);
                return false;
            }
        }

        /// <summary>取图片的替代文字（alt），用于图题注。</summary>
        public static string ExtractAltText(LinkInline image)
        {
            StringBuilder sb = new();
            AppendLiterals(image, sb);
            return sb.ToString().Trim();
        }

        private static void AppendLiterals(ContainerInline container, StringBuilder sb)
        {
            foreach (Inline inline in container)
            {
                if (inline is LiteralInline literal)
                {
                    sb.Append(literal.Content.ToString());
                }
                else if (inline is CodeInline code)
                {
                    sb.Append(code.Content);
                }
                else if (inline is ContainerInline child)
                {
                    AppendLiterals(child, sb);
                }
            }
        }

        private static void AppendPlaceholder(OpenXmlElement parent, string? url, DocxRunStyle style)
        {
            DocxInlineRenderer.AppendText(parent, $"[图片未嵌入: {url}]", style.AsItalic());
        }

        private static bool TryResolveLocalImage(string? url, string? baseDir, out string fullPath, out PartTypeInfo type)
        {
            fullPath = string.Empty;
            type = ImagePartType.Png;

            if (string.IsNullOrWhiteSpace(url))
            {
                return false;
            }

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                || url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string path = Uri.UnescapeDataString(url);
            try
            {
                if (!Path.IsPathRooted(path))
                {
                    if (string.IsNullOrEmpty(baseDir))
                    {
                        return false;
                    }

                    path = Path.Combine(baseDir, path);
                }

                path = Path.GetFullPath(path);
            }
            catch (ArgumentException)
            {
                return false;
            }

            if (!File.Exists(path) || !TryMapImageType(Path.GetExtension(path), out type))
            {
                return false;
            }

            fullPath = path;
            return true;
        }

        private static bool TryMapImageType(string extension, out PartTypeInfo type)
        {
            switch (extension.ToUpperInvariant())
            {
                case ".PNG": type = ImagePartType.Png; return true;
                case ".JPG" or ".JPEG": type = ImagePartType.Jpeg; return true;
                case ".GIF": type = ImagePartType.Gif; return true;
                case ".BMP": type = ImagePartType.Bmp; return true;
                case ".TIF" or ".TIFF": type = ImagePartType.Tiff; return true;
                default: type = ImagePartType.Png; return false;
            }
        }

        private static bool TryGetPixelSize(byte[] bytes, out int pixelWidth, out int pixelHeight, out double dpiX, out double dpiY)
        {
            pixelWidth = 0;
            pixelHeight = 0;
            dpiX = 96;
            dpiY = 96;
            try
            {
                using MemoryStream ms = new(bytes);
                BitmapDecoder decoder = BitmapDecoder.Create(ms, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                if (decoder.Frames.Count == 0)
                {
                    return false;
                }

                BitmapFrame frame = decoder.Frames[0];
                pixelWidth = frame.PixelWidth;
                pixelHeight = frame.PixelHeight;
                dpiX = frame.DpiX > 0 ? frame.DpiX : 96;
                dpiY = frame.DpiY > 0 ? frame.DpiY : 96;
                return pixelWidth > 0 && pixelHeight > 0;
            }
            catch (Exception ex) when (ex is NotSupportedException or ArgumentException or FileFormatException or OverflowException)
            {
                return false;
            }
        }

        private static (long Cx, long Cy) ComputeEmu(int pixelWidth, int pixelHeight, double dpiX, double dpiY)
        {
            long cx = (long)(pixelWidth / dpiX * EmuPerInch);
            long cy = (long)(pixelHeight / dpiY * EmuPerInch);

            if (cx > MaxWidthEmu)
            {
                double scale = (double)MaxWidthEmu / cx;
                cx = MaxWidthEmu;
                cy = (long)(cy * scale);
            }

            return (cx > 0 ? cx : EmuPerInch, cy > 0 ? cy : EmuPerInch);
        }

        private static Drawing BuildDrawing(string relationshipId, long cx, long cy, uint drawingId, string name)
        {
            return new Drawing(
                new DW.Inline(
                    new DW.Extent { Cx = cx, Cy = cy },
                    new DW.EffectExtent { LeftEdge = 0L, TopEdge = 0L, RightEdge = 0L, BottomEdge = 0L },
                    new DW.DocProperties { Id = drawingId, Name = "Picture " + drawingId },
                    new DW.NonVisualGraphicFrameDrawingProperties(new A.GraphicFrameLocks { NoChangeAspect = true }),
                    new A.Graphic(
                        new A.GraphicData(
                            new PIC.Picture(
                                new PIC.NonVisualPictureProperties(
                                    new PIC.NonVisualDrawingProperties { Id = 0U, Name = name },
                                    new PIC.NonVisualPictureDrawingProperties()),
                                new PIC.BlipFill(
                                    new A.Blip { Embed = relationshipId },
                                    new A.Stretch(new A.FillRectangle())),
                                new PIC.ShapeProperties(
                                    new A.Transform2D(
                                        new A.Offset { X = 0L, Y = 0L },
                                        new A.Extents { Cx = cx, Cy = cy }),
                                    new A.PresetGeometry(new A.AdjustValueList()) { Preset = A.ShapeTypeValues.Rectangle })))
                        { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }))
                {
                    DistanceFromTop = 0U,
                    DistanceFromBottom = 0U,
                    DistanceFromLeft = 0U,
                    DistanceFromRight = 0U,
                });
        }
    }
}

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
        private const long MaxImageBytes = 50L * 1024 * 1024;   // 单张图片字节上限，防超大文件/解压炸弹耗尽内存
        private const long MaxImagePixels = 100_000_000L;       // 解码后像素总数上限（约 1 亿，10000×10000）

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
                // 读取前先按文件大小拦截：超大文件不读入内存，避免解压炸弹/巨图耗尽内存
                if (new FileInfo(fullPath).Length > MaxImageBytes)
                {
                    AppendPlaceholder(parent, image.Url, style);
                    return false;
                }

                byte[] bytes = File.ReadAllBytes(fullPath);
                if (!TryGetPixelSize(bytes, out int pixelWidth, out int pixelHeight, out double dpiX, out double dpiY))
                {
                    AppendPlaceholder(parent, image.Url, style);
                    return false;
                }

                // 像素总数超上限（声明维度极大的解压炸弹）改为占位文字，避免 Word 端解码再次放大内存
                if ((long)pixelWidth * pixelHeight > MaxImagePixels)
                {
                    AppendPlaceholder(parent, image.Url, style);
                    return false;
                }

                // 按文件头魔数嗅探真实格式覆盖按扩展名得到的 type：扩展名与内容不符(如 .png 实为 jpeg)时
                // 若仍按扩展名声明 ImagePart 类型，Word 端会因内容不匹配破图
                if (TrySniffImageType(bytes, out PartTypeInfo sniffed))
                {
                    type = sniffed;
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
                // 安全：仅允许 baseDir 之内的相对路径图片，拒绝绝对路径与 ../ 穿越，
                // 防止不受信 Markdown 借 ![](C:/...) 或 ![](../../) 读取任意本地文件并嵌入导出文档
                if (Path.IsPathRooted(path) || string.IsNullOrEmpty(baseDir))
                {
                    return false;
                }

                string fullBase = Path.GetFullPath(baseDir);
                string baseWithSep = fullBase.EndsWith(Path.DirectorySeparatorChar)
                    ? fullBase
                    : fullBase + Path.DirectorySeparatorChar;

                path = Path.GetFullPath(Path.Combine(fullBase, path));
                if (!path.StartsWith(baseWithSep, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }

                // 进一步防符号链接/junction 穿越：GetFullPath/StartsWith 仅做文本前缀判断，
                // 不解析链接。若路径中任一级是指向 baseDir 之外的链接（如 pics→C:\...\.ssh），
                // 文本上仍落在 baseWithSep 内 → 解析真实目标后再次校验前缀，拒绝越界。
                string? realPath = ResolveFinalPath(path);
                if (realPath == null
                    || !Path.GetFullPath(realPath).StartsWith(baseWithSep, StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
                path = realPath;
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

        // 按文件头魔数识别真实图片格式（与 TryMapImageType 的扩展名映射互补，用于覆盖名实不符的情况）
        private static bool TrySniffImageType(byte[] bytes, out PartTypeInfo type)
        {
            type = ImagePartType.Png;
            if (bytes.Length < 4)
            {
                return false;
            }

            // PNG: 89 50 4E 47
            if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
            {
                type = ImagePartType.Png;
                return true;
            }
            // JPEG: FF D8 FF
            if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF)
            {
                type = ImagePartType.Jpeg;
                return true;
            }
            // GIF: "GIF8"
            if (bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46 && bytes[3] == 0x38)
            {
                type = ImagePartType.Gif;
                return true;
            }
            // BMP: "BM"
            if (bytes[0] == 0x42 && bytes[1] == 0x4D)
            {
                type = ImagePartType.Bmp;
                return true;
            }
            // TIFF: "II*\0" (little-endian) 或 "MM\0*" (big-endian)
            if ((bytes[0] == 0x49 && bytes[1] == 0x49 && bytes[2] == 0x2A && bytes[3] == 0x00)
                || (bytes[0] == 0x4D && bytes[1] == 0x4D && bytes[2] == 0x00 && bytes[3] == 0x2A))
            {
                type = ImagePartType.Tiff;
                return true;
            }

            return false;
        }

        // 把绝对路径中的每一级目录与最终文件沿符号链接/junction 解析到真实物理路径。
        // 逐级解析可覆盖"中间目录是 junction、文件本身非链接"的穿越（File.ResolveLinkTarget 只看末段）。
        // 任一级解析抛异常或目标无法定位时返回 null，由调用方按"拒绝"处理（fail-closed）。
        private static string? ResolveFinalPath(string fullPath)
        {
            try
            {
                string? root = Path.GetPathRoot(fullPath);
                if (string.IsNullOrEmpty(root))
                {
                    return null;
                }

                // 从根开始逐段拼接并解析链接；relative 为去掉根后的各级名
                string relative = fullPath[root.Length..];
                string[] segments = relative.Split(
                    [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                    StringSplitOptions.RemoveEmptyEntries);

                string current = root;
                foreach (string segment in segments)
                {
                    current = Path.Combine(current, segment);

                    // 该级若为符号链接/junction，解析其最终目标（returnFinalTarget 跟随链式链接）
                    FileSystemInfo? info = Directory.Exists(current)
                        ? new DirectoryInfo(current)
                        : File.Exists(current) ? new FileInfo(current) : null;

                    string? linkTarget = info?.ResolveLinkTarget(returnFinalTarget: true)?.FullName;
                    if (linkTarget != null)
                    {
                        // 链接目标可能是相对路径，统一规范化为绝对路径继续向下解析
                        current = Path.GetFullPath(linkTarget);
                    }
                }

                return current;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or System.Security.SecurityException)
            {
                return null;
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
                dpiX = frame.DpiX switch
                {
                    > 0 => frame.DpiX,
                    _ => 96
                };
                dpiY = frame.DpiY switch
                {
                    > 0 => frame.DpiY,
                    _ => 96
                };
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
            // 宽度超过正文可用宽度时：先按原始宽度比例缩小高度，再把宽度钳到上限，避免图片溢出页面同时保持宽高比
            if (cx > MaxWidthEmu)
            {
                cy = (long)(cy * ((double)MaxWidthEmu / cx));
                cx = MaxWidthEmu;
            }

            return (cx switch
            {
                > 0 => cx,
                _ => EmuPerInch
            }, cy switch
            {
                > 0 => cy,
                _ => EmuPerInch
            });
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

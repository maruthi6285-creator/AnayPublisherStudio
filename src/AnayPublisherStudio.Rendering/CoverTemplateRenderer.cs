using AnayPublisherStudio.Domain.Model;
using AnayPublisherStudio.Domain.ValueObjects;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SkiaSharp;

namespace AnayPublisherStudio.Rendering;

public static class CoverTemplateRenderer
{
    public static void RenderTemplate(PublishingProject project, PublishingTemplate template, int pageCount, Stream output)
    {
        RenderingLicense.Configure();

        var ow = template.OverallWidth;
        var oh = template.OverallHeight;
        var tw = template.TrimWidth;
        var th = template.TrimHeight;
        var sw = template.SpineWidth;
        var b = template.BleedInches;
        var sz = template.CoverSafeZoneInches;
        var bW = template.BarcodeWidth;
        var bH = template.BarcodeHeight;

        var panelW = (ow - sw) / 2;
        var spineX = panelW;
        var frontX = panelW + sw;

        var barcodeX = (ow - frontX) + (tw - bW - sz);
        var barcodeY = oh - b - sz - bH;

        using var ms = new MemoryStream();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(new PageSize((float)ow, (float)oh, Unit.Inch));
                page.Margin(0);

                page.Content().Background(Colors.White);

                page.Content().Canvas((object canvas, Size size) =>
                {
                    var sk = (SKCanvas)canvas;
                    float px(double v) => (float)(v * 72);

                    var pinkFill = new SKColor(0xFF, 0xB6, 0xC1, 0x30);
                    var magenta = new SKColor(0xFF, 0x00, 0xFF, 0xFF);
                    var cyan = new SKColor(0x00, 0xFF, 0xFF, 0xFF);
                    var green = new SKColor(0x00, 0xFF, 0x00, 0xFF);
                    var gold = new SKColor(0xFF, 0xD7, 0x00, 0xFF);
                    var goldFill = new SKColor(0xFF, 0xD7, 0x00, 0x20);
                    var spineBg = new SKColor(0xCC, 0xCC, 0xCC, 0xFF);
                    var grey = new SKColor(0x66, 0x66, 0x66, 0xFF);
                    var hotPink = new SKColor(0xFF, 0x69, 0xB4, 0xFF);

                    float bp = px(b);
                    float tp = px(tw);
                    float thp = px(th);
                    float sp = px(sw);
                    float sx = px(spineX);
                    float bxp = px(barcodeX);
                    float byp = px(barcodeY);
                    float bWp = px(bW);
                    float bHp = px(bH);
                    float szp = px(sz);
                    float w = size.Width;
                    float h = size.Height;

                    // Pink bleed bands
                    using var pinkPaint = new SKPaint { Color = pinkFill, Style = SKPaintStyle.Fill };
                    sk.DrawRect(0, 0, w, bp, pinkPaint);
                    sk.DrawRect(0, h - bp, w, bp, pinkPaint);
                    sk.DrawRect(0, 0, bp, h, pinkPaint);
                    sk.DrawRect(w - bp, 0, bp, h, pinkPaint);

                    // Spine background
                    using var spinePaint = new SKPaint { Color = spineBg, Style = SKPaintStyle.Fill };
                    sk.DrawRect(sx, bp, sp, thp, spinePaint);

                    // Barcode fill
                    using var barcodePaint = new SKPaint { Color = goldFill, Style = SKPaintStyle.Fill };
                    sk.DrawRect(bxp, h - byp - bHp, bWp, bHp, barcodePaint);

                    // Trim box (dashed magenta)
                    using var dashEffect = SKPathEffect.CreateDash([5, 3], 0);
                    using var magentaPaint = new SKPaint
                    {
                        Color = magenta, Style = SKPaintStyle.Stroke, StrokeWidth = 1.5f,
                        PathEffect = dashEffect
                    };
                    sk.DrawRect(bp, bp, tp, thp, magentaPaint);

                    // Spine edges (dashed cyan)
                    using var cyanDash = SKPathEffect.CreateDash([4, 4], 0);
                    using var cyanPaint = new SKPaint
                    {
                        Color = cyan, Style = SKPaintStyle.Stroke, StrokeWidth = 1,
                        PathEffect = cyanDash
                    };
                    sk.DrawLine(sx, bp, sx, bp + thp, cyanPaint);
                    sk.DrawLine(sx + sp, bp, sx + sp, bp + thp, cyanPaint);

                    // Safe zone (dashed green)
                    using var greenDash = SKPathEffect.CreateDash([3, 3], 0);
                    using var greenPaint = new SKPaint
                    {
                        Color = green, Style = SKPaintStyle.Stroke, StrokeWidth = 1,
                        PathEffect = greenDash
                    };
                    sk.DrawRect(bp + szp, bp + szp, tp - 2 * szp, thp - 2 * szp, greenPaint);

                    // Barcode border (solid gold)
                    using var goldPaint = new SKPaint
                    {
                        Color = gold, Style = SKPaintStyle.Stroke, StrokeWidth = 2
                    };
                    sk.DrawRect(bxp, h - byp - bHp, bWp, bHp, goldPaint);

                    // Crop marks (solid magenta, thin)
                    using var cropPaint = new SKPaint
                    {
                        Color = magenta, Style = SKPaintStyle.Stroke, StrokeWidth = 0.5f
                    };
                    float cm = 12;
                    float trX = bp + tp;
                    float blY = h - bp;

                    sk.DrawLine(bp - cm, bp, bp + cm * 2, bp, cropPaint);
                    sk.DrawLine(bp, bp - cm, bp, bp + cm * 2, cropPaint);
                    sk.DrawLine(trX - cm * 2, bp, trX + cm, bp, cropPaint);
                    sk.DrawLine(trX, bp - cm, trX, bp + cm * 2, cropPaint);
                    sk.DrawLine(bp - cm, blY, bp + cm * 2, blY, cropPaint);
                    sk.DrawLine(bp, blY - cm * 2, bp, blY + cm, cropPaint);
                    sk.DrawLine(trX - cm * 2, blY, trX + cm, blY, cropPaint);
                    sk.DrawLine(trX, blY - cm * 2, trX, blY + cm, cropPaint);

                    // Labels
                    using var labelFont = new SKFont(SKTypeface.FromFamilyName("Arial"), 9);
                    using var labelPaint = new SKPaint { Color = magenta, Style = SKPaintStyle.Fill };

                    void DrawLabel(float x, float y, string text, SKColor color, float fontSize = 9)
                    {
                        using var f = new SKFont(SKTypeface.FromFamilyName("Arial"), fontSize);
                        using var p = new SKPaint { Color = color, Style = SKPaintStyle.Fill, IsAntialias = true };
                        sk.DrawText(text, x, y, SKTextAlign.Left, f, p);
                    }

                    void DrawLabelCenter(float cx, float y, string text, SKColor color, float fontSize = 9)
                    {
                        using var f = new SKFont(SKTypeface.FromFamilyName("Arial"), fontSize);
                        using var p = new SKPaint { Color = color, Style = SKPaintStyle.Fill, IsAntialias = true };
                        var textWidth = f.MeasureText(text);
                        sk.DrawText(text, cx - textWidth / 2, y, SKTextAlign.Left, f, p);
                    }

                    float lx = bp + tp / 2;

                    DrawLabelCenter(lx, bp - 5, "TRIM", magenta, 8);
                    DrawLabelCenter(lx, h - bp + 12, "TRIM", magenta, 8);
                    DrawLabelCenter(bp + szp + (tp - 2 * szp) / 2, bp + szp - 5, "SAFE ZONE", green, 8);
                    DrawLabelCenter(bxp + bWp / 2, h - byp - bHp - 5, "BARCODE ZONE", gold, 8);
                    DrawLabelCenter(sx + sp / 2, bp + 12, $"SPINE ({sw:F2}\")", cyan, 8);
                    DrawLabel(bp - 40, bp + thp / 2 + 3, $"BACK ({tw:F0}\")", magenta, 8);
                    DrawLabel(w - bp + 10, bp + thp / 2 + 3, $"FRONT ({tw:F0}\")", magenta, 8);
                    DrawLabelCenter(bp / 2, bp - 5, "BLEED", hotPink, 7);
                    DrawLabelCenter(w - bp / 2, bp - 5, "BLEED", hotPink, 7);

                    DrawLabelCenter(w / 2, h - 5,
                        $"KDP {tw:F0}x{th:F0} | {template.Paper} | Spine: {sw:F3}\" | {pageCount}p",
                        grey, 8);
                });
            });
        }).GeneratePdf(ms);

        var pdfBytes = ms.ToArray();
        PdfMetadataInjector.Inject(
            pdfBytes,
            $"Template: {project.Metadata.Title}",
            project.Metadata.Author ?? "Unknown",
            $"Cover Template - {template.Platform} {template.TrimWidth}x{template.TrimHeight}",
            output);
    }
}

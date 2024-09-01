using System.CommandLine.Invocation;
using System.Drawing;
using Cosmic.CommandLine;
using Cosmic.CommandLine.Attributes;
using SkiaSharp;

namespace Titanium.Commands;

[CliCommand("sample", "Generates sample images for testing.")]
public class SampleCommand : CliCommand
{
  

    public static void DrawTextOnImage(string text, string outputImagePath, int width = 500, int height = 300)
    {
        // Create a new bitmap with the specified dimensions
        using (SKBitmap bitmap = new SKBitmap(width, height))
        {
            // Initialize a canvas to draw on the bitmap
            using (SKCanvas canvas = new SKCanvas(bitmap))
            {
                // Set the background color to white
                canvas.Clear(SKColors.White);

                // Define the font, paint, and text alignment
                using (SKPaint paint = new SKPaint())
                {
                    paint.TextSize = 40.0f;
                    paint.IsAntialias = true;
                    paint.Color = SKColors.Black;
                    paint.IsStroke = false;
                    paint.TextAlign = SKTextAlign.Center;

                    // Calculate the position to center the text
                    SKRect textBounds = new SKRect();
                    paint.MeasureText(text, ref textBounds);
                    float x = width / 2;
                    float y = (height + textBounds.Height) / 2;

                    // Draw the text onto the canvas
                    canvas.DrawText(text, x, y, paint);
                }
            }

            // Save the bitmap as a PNG file
            using (SKImage image = SKImage.FromBitmap(bitmap))
            using (SKData data = image.Encode(SKEncodedImageFormat.Png, 100))
            {
                System.IO.File.WriteAllBytes(outputImagePath, data.ToArray());
            }

        }
    }

    protected override Task<int> ExecuteCommand(CliCommandContext context)
    {
        DrawTextOnImage("Hello, World!", "output.png");
        return Task.FromResult(0);
    }
}
using System;
using ImageMagick;

namespace VirtualPeto.Tools
{
    public class SpriteExtractor
    {
        public void ExtractToGif(string spriteSheetPath, string outputPath, int columns, int rows, int delayCs = 10, int? width = null, int? height = null, int? maxFrames = null)
        {
            using (MagickImageCollection collection = new MagickImageCollection())
            {
                using (MagickImage spriteSheet = new MagickImage(spriteSheetPath))
                {
                    int frameWidth = (int)(spriteSheet.Width / columns);
                    int frameHeight = (int)(spriteSheet.Height / rows);
                    int targetWidth = width.GetValueOrDefault();
                    int targetHeight = height.GetValueOrDefault();
                    int frameLimit = maxFrames.GetValueOrDefault();
                    int addedFrames = 0;

                    for (int y = 0; y < rows; y++)
                    {
                        for (int x = 0; x < columns; x++)
                        {
                            if (frameLimit > 0 && addedFrames >= frameLimit) break;
                            MagickImage frame = new MagickImage(spriteSheet);
                            frame.Crop(new MagickGeometry(x * frameWidth, y * frameHeight, (uint)frameWidth, (uint)frameHeight));
                            frame.ResetPage();
                            if (targetWidth > 0 && targetHeight > 0)
                            {
                                frame.Resize(new MagickGeometry((uint)targetWidth, (uint)targetHeight)
                                {
                                    IgnoreAspectRatio = false
                                });
                                frame.Extent((uint)targetWidth, (uint)targetHeight, Gravity.Center, MagickColors.Transparent);
                            }
                            frame.AnimationDelay = (uint)Math.Max(1, delayCs);
                            frame.GifDisposeMethod = GifDisposeMethod.Background;
                            collection.Add(frame);
                            addedFrames++;
                        }
                        if (frameLimit > 0 && addedFrames >= frameLimit) break;
                    }
                }

                if (collection.Count == 0) return;
                collection[0].AnimationIterations = 0;
                collection.Optimize();
                collection.Write(outputPath);
            }
        }
    }
}
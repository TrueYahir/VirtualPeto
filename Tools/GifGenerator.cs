using System;
using System.Collections.Generic;
using ImageMagick;

namespace VirtualPeto.Tools
{
    public class GifGenerator
    {
        public void CreateFromImages(IEnumerable<string> imagePaths, string outputPath, int delay = 10, int? width = null, int? height = null, int? maxFrames = null)
        {
            int targetWidth = width.GetValueOrDefault();
            int targetHeight = height.GetValueOrDefault();
            int framesLimit = maxFrames.GetValueOrDefault();

            using (MagickImageCollection collection = new MagickImageCollection())
            {
                int addedFrames = 0;
                foreach (string path in imagePaths)
                {
                    if (framesLimit > 0 && addedFrames >= framesLimit) break;
                    MagickImage frame = new MagickImage(path);
                    if (targetWidth > 0 && targetHeight > 0)
                    {
                        frame.Resize(new MagickGeometry((uint)targetWidth, (uint)targetHeight)
                        {
                            IgnoreAspectRatio = false
                        });
                        frame.Extent((uint)targetWidth, (uint)targetHeight, Gravity.Center, MagickColors.Transparent);
                    }
                    frame.AnimationDelay = (uint)delay;
                    frame.GifDisposeMethod = GifDisposeMethod.Background;
                    collection.Add(frame);
                    addedFrames++;
                }
                
                if (collection.Count > 0)
                {
                    collection[0].AnimationIterations = (uint)0;
                    collection.Optimize();
                    collection.Write(outputPath);
                }
            }
        }
    }
}
using ImageProcessor;
using ImageProcessor.Common.Exceptions;
using ImageProcessor.Processors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;

namespace SAEON.ImageProcessing
{
    public class Overlay : IGraphicsProcessor
    {
        /// <summary>
        /// Gets or sets DynamicParameter.
        /// </summary>
        public dynamic DynamicParameter { get; set; }

        /// <summary> 
        /// Gets or sets any additional settings required by the processor.
        /// </summary>
        public Dictionary<string, string> Settings { get; set; }

        public Overlay()
        {
            this.Settings = new Dictionary<string, string>();
        }

        /// <summary>
        /// Processes the image.
        /// </summary>
        /// <param name="factory">
        /// The current instance of the <see cref="T:ImageProcessor.ImageFactory"/> class containing
        /// the image to process.
        /// </param>
        /// <returns>
        /// The processed image from the current instance of the <see cref="T:ImageProcessor.ImageFactory"/> class.
        /// </returns>
        public Image ProcessImage(ImageFactory factory)
        {
            string fileName = DynamicParameter.FileName ?? string.Empty;
            int width = DynamicParameter.Size.Width ?? 0;
            int height = DynamicParameter.Size.Height ?? 0;
            int left = DynamicParameter.Position.X ?? 0;
            int top = DynamicParameter.Position.Y ?? 0;

            FileInfo fileInfo = new FileInfo(fileName);
            if (!fileInfo.Exists) throw new FileNotFoundException(fileName);

            Bitmap overlayImage = null;
            Image image = factory.Image;
            try
            {
                overlayImage = (Bitmap)Image.FromFile(fileName);
                if ((height == 0) && (width == 0))
                {
                    height = overlayImage.Height;
                    width = overlayImage.Width;
                }
                else if ((height == 0) && (width != 0))
                { // calculate height based on width
                    height = overlayImage.Height * width / overlayImage.Width;
                }
                else if ((height != 0) && (width == 0))
                { // calculate width based on height
                    width = overlayImage.Width * height / overlayImage.Height;
                }
                using (var graphics = Graphics.FromImage(image))
                {
                    graphics.CompositingQuality = CompositingQuality.HighQuality;
                    //graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.CompositingMode = CompositingMode.SourceOver;
                    //graphics.DrawImageUnscaled(overlayImage, 0, 0, width, height);
                    graphics.DrawImage(overlayImage, left, top, width, height);
                }
            }
            catch (Exception ex)
            {
                throw new ImageProcessingException("Error processing image with " + this.GetType().Name, ex);
            }
            finally
            {
                if (overlayImage != null) overlayImage.Dispose();
            }
            return image;
        }
    }
}

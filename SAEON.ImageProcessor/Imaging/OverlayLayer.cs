using System;
using System.Drawing;

namespace SAEON.ImageProcessor.Imaging
{
    public class OverlayLayer
    {
        public string FileName { get; set; }
        public Point Position { get; set; }
        public Size Size { get; set; }

        public OverlayLayer(string fileName, Point postition, Size size)
        {
            FileName = fileName;
            Position = postition;
            Size = size;
        }

        public override bool Equals(object obj)
        {
            OverlayLayer overlayLayer = obj as OverlayLayer;

            if (overlayLayer == null) return false;

            // Define the tolerance for variation in their values 
            return FileName.Equals(overlayLayer.FileName, StringComparison.CurrentCultureIgnoreCase) &&
                Position.Equals(overlayLayer.Position) &&
                Size.Equals(overlayLayer.Size);
        }

        public override int GetHashCode()
        {
            return FileName.GetHashCode() +
                Position.GetHashCode() +
                Size.GetHashCode();
        }
    }
}

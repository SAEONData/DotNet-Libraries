using ImageProcessor;
using ImageProcessor.Imaging.MetaData;
using Microsoft.WindowsAPICodePack.Shell;
using System;
using System.Collections.Generic; 
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Reflection;
using System.Text;

namespace SAEON.ImageProcessing
{
    public static class ImageFactoryExtensions
    {
        public static ImageFactory Overlay(this ImageFactory factory, string fileName, Point position, Size size)
        {
            if (factory.ShouldProcess)
            {
                OverlayLayer overlayLayer = new OverlayLayer(fileName, position, size);
                return factory.Overlay(overlayLayer);
            }
            return factory;
        }

        public static ImageFactory Overlay(this ImageFactory factory, string fileName, Point position)
        {
            if (factory.ShouldProcess)
            {
                OverlayLayer overlayLayer = new OverlayLayer(fileName, position, new Size(0, 0));
                return factory.Overlay(overlayLayer);
            }
            return factory;
        }

        public static ImageFactory Overlay(this ImageFactory factory, OverlayLayer overlayLayer)
        {
            if (factory.ShouldProcess)
            {
                Overlay overlay = new Overlay { DynamicParameter = overlayLayer };
                factory.CurrentImageFormat.ApplyProcessor(overlay.ProcessImage, factory);
            }
            return factory;
        }

        public static ImageFactory GetExifString(this ImageFactory imageFactory, ExifPropertyTag propertyTag, out string value)
        {
            value = null;
            if (imageFactory.ExifPropertyItems.TryGetValue((int)propertyTag, out PropertyItem propertyItem) &&
                (propertyItem != null) &&
                (propertyItem.Type == (int)ExifPropertyTagType.ASCII))
                value = Encoding.UTF8.GetString(propertyItem.Value).Replace("\0", "");
            return imageFactory;
        }

        public static ImageFactory GetExifLatLong(this ImageFactory imageFactory, ExifPropertyTag propertyTagRef, ExifPropertyTag propertyTag, out decimal? value)
        {
            value = null;
            imageFactory.GetExifString(propertyTagRef, out string latLongRef);
            if (imageFactory.ExifPropertyItems.TryGetValue((int)propertyTag, out PropertyItem propertyItem) &&
                (propertyItem != null) &&
                (propertyItem.Type == (int)ExifPropertyTagType.Rational))
            {
                decimal degreesNumerator = BitConverter.ToUInt32(propertyItem.Value, 0);
                decimal degreesDenominator = BitConverter.ToUInt32(propertyItem.Value, 4);
                decimal degrees = degreesNumerator / (decimal)degreesDenominator;

                decimal minutesNumerator = BitConverter.ToUInt32(propertyItem.Value, 8);
                decimal minutesDenominator = BitConverter.ToUInt32(propertyItem.Value, 12);
                decimal minutes = minutesNumerator / (decimal)minutesDenominator;

                decimal secondsNumerator = BitConverter.ToUInt32(propertyItem.Value, 16);
                decimal secondsDenominator = BitConverter.ToUInt32(propertyItem.Value, 20);
                decimal seconds = secondsNumerator / (decimal)secondsDenominator;

                value = degrees + (minutes / 60m) + (seconds / 3600m);
                if (!string.IsNullOrEmpty(latLongRef) && ((latLongRef == "S") || (latLongRef == "W"))) value *= -1m;
            }
            return imageFactory;
        }

        public static ImageFactory GetTags(this ImageFactory imageFactory, string fileName, out List<string> tags)
        {
            string[] fileTags = ShellFile.FromFilePath(fileName).Properties.System.Keywords.Value;
            if (fileTags == null)
                tags = new List<string>();
            else
                tags = fileTags.ToList();
            return imageFactory;
        }

        private static PropertyItem CreatePropertyItem()
        {
            var ci = typeof(PropertyItem);
            var o = ci.GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public, null, new Type[] { }, null);

            return (PropertyItem)o.Invoke(null);
        }

        public static ImageFactory SetExifString(this ImageFactory imageFactory, ExifPropertyTag tag, string value)
        {
            value += "\0";
            if (imageFactory.ExifPropertyItems.TryGetValue((int)tag, out PropertyItem propertyItem) && (propertyItem != null))
            {
                if (propertyItem.Type != (int)ExifPropertyTagType.ASCII) propertyItem.Type = (int)ExifPropertyTagType.ASCII;
                propertyItem.Value = Encoding.UTF8.GetBytes(value);
                propertyItem.Len = propertyItem.Value.Length;
                imageFactory.Image.SetPropertyItem(propertyItem);
            }
            else
            {
                propertyItem = CreatePropertyItem();
                propertyItem.Id = (int)tag;
                propertyItem.Type = (int)ExifPropertyTagType.ASCII;
                propertyItem.Value = Encoding.UTF8.GetBytes(value);
                propertyItem.Len = propertyItem.Value.Length;
                imageFactory.Image.SetPropertyItem(propertyItem);
                imageFactory.ExifPropertyItems.TryAdd((int)tag, propertyItem);
            }
            return imageFactory;
        }

        public static ImageFactory AddTags(this ImageFactory imageFactory, string fileName, List<string> newTags)
        {
            imageFactory.GetTags(fileName, out List<string> tags);
            if (tags == null)
                imageFactory.SetTags(fileName, newTags);
            else
            {
                foreach (var tag in newTags)
                    if (!tags.Contains(tag)) tags.Add(tag);
                imageFactory.SetTags(fileName, tags);
            }
            return imageFactory;
        }

        public static ImageFactory SetTags(this ImageFactory imageFactory, string fileName, List<string> tags)
        {
            ShellFile.FromFilePath(fileName).Properties.System.Keywords.Value = tags.ToArray();
            return imageFactory;
        }
    }
}

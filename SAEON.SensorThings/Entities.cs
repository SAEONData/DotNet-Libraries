using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SAEON.SensorThings
{
    public static class ValueCodes
    {
        public static readonly string GeoJson = "application/vnd.geo+json";
        public static readonly string Pdf = "application/pdf";
        public static readonly string OM_Measurement = "http://www.opengis.net/def/observationType/OGC-OM/2.0/OM_Measurement";
    }

    public class Coordinate
    {
        public double? Longitude { get; set; } = null;
        public double? Latitude { get; set; } = null;
        public double? Elevation { get; set; } = null;

        public JToken AsJSON
        {
            get
            {
                var result = new JArray();
                if (Latitude.HasValue && Longitude.HasValue)
                {
                    result.Add(Longitude.Value);
                    result.Add(Latitude.Value);
                    if (Elevation.HasValue)
                    {
                        result.Add(Elevation.Value);
                    }
                };
                return result;
            }
        }
    }

    /*
    public class BoundingBox
    {
        [Required]
        public decimal Left { get; set; }
        [Required]
        public decimal Bottom { get; set; }
        [Required]
        public decimal Right { get; set; }
        [Required]
        public decimal Top { get; set; }

        //public override string ToString()
        //{
        //}
    }

    public class TimeInterval
    {
        [Required]
        public DateTime Start { get; set; }
        [Required]
        public DateTime End { get; set; }

        public override string ToString()
        {
            return $"{Start.ToString("o")}/{End.ToString("o")}";
        }
    }

    public class TimeOrInterval
    {
        [Required]
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }

        public override string ToString()
        {
            var result = $"{Start.ToString("o")}";
            if (End.HasValue)
            {
                result += $"/{End.Value.ToString("o")}";
            }
            return result;
        }
    }
    */

    public abstract class SensorThingEntity
    {
        public Uri Uri { get; set; }

        public int Id { get; set; }
        public string SelfLink => $"{Uri.GetLeftPart(UriPartial.Path)}/({Id})";
        public List<string> NavigationLinks { get; private set; } = new List<string>();

        public virtual JObject AsJSON
        {
            get
            {
                var result = new JObject
                {
                    new JProperty("@iot.id", Id),
                    new JProperty("@iot.selfLink", SelfLink)
                };
                foreach (var link in NavigationLinks)
                {
                    result.Add($"{link}@iot.navigationLink", $"{Uri.AbsolutePath}({Id})/{link}");
                }
                return result;
            }
        }
    }

    public abstract class NamedSensorThingEntity : SensorThingEntity
    {
        [Required]
        public string Name { get; set; }
        [Required]
        public string Description { get; set; }

        public override JObject AsJSON
        {
            get
            {
                var result = base.AsJSON;
                result.Add(new JProperty("name", Name));
                result.Add(new JProperty("description", Description));
                return result;
            }
        }
    }

    public class Thing : NamedSensorThingEntity
    {
        public Dictionary<string, object> Properties { get; private set; } = new Dictionary<string, object>();

        public Thing() : base()
        {
            NavigationLinks.Add("Locations");
            NavigationLinks.Add("HistoricalLocations");
            NavigationLinks.Add("Datastreams");
        }

        public override JObject AsJSON
        {
            get
            {
                var result = base.AsJSON;
                if (Properties.Any())
                {
                    var properties = new JObject();
                    foreach (var property in Properties)
                    {
                        properties.Add(new JProperty(property.Key, property.Value));
                    }
                    result.Add(new JProperty("properties", properties));
                }
                return result;
            }
        }
    }

    public class Location : NamedSensorThingEntity
    {
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double? Elevation { get; set; } = null;

        public Location() : base()
        {
            NavigationLinks.Add("Things");
            NavigationLinks.Add("HistoricalLocations");
        }

        private JArray CoordinatesAsJSON
        {
            get
            {
                var result = new JArray(new JValue(Longitude), new JValue(Latitude));
                if (Elevation.HasValue) result.Add(new JValue(Elevation.Value));
                return result;
            }
        }
        public override JObject AsJSON
        {
            get
            {
                var result = base.AsJSON;
                result.Add(new JProperty("encodingType", ValueCodes.GeoJson));
                var jLocation = new JObject(
                    new JProperty("type", "Feature"), 
                    new JProperty("geometry", new JObject(
                        new JProperty("type", "Point"),
                        new JProperty("coordinates", CoordinatesAsJSON))));
                return result;
            }
        }
    }

}

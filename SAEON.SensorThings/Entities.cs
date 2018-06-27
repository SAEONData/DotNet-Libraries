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

    public class UnitOfMeasurement
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string Definition { get; set; }
    }

    public class BoundingBox
    {
        public decimal Left { get; set; }
        public decimal Bottom { get; set; }
        public decimal Right { get; set; }
        public decimal Top { get; set; }
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

    public abstract class SensorThingEntity
    {
        public string BaseUrl { get; set; }
        public string EntitySetName { get; protected set; }

        public int Id { get; set; }
        public string SelfLink => $"{BaseUrl}/{EntitySetName}({Id})";
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
                    result.Add($"{link}@iot.navigationLink", $"{BaseUrl}/{EntitySetName}({Id})/{link}");
                }
                return result;
            }
        }

        public object Call { get; private set; }
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
        public Location Location { get; set; } = null;
        public List<HistoricalLocation> HistoricalLocations { get; } = new List<HistoricalLocation>();

        public Thing() : base()
        {
            EntitySetName = "Things";
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
        public List<Thing> Things { get; } = new List<Thing>();
        public List<HistoricalLocation> HistoricalLocations { get; } = new List<HistoricalLocation>();

        public Location() : base()
        {
            EntitySetName = "Locations";
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
                result.Add(new JProperty("location", new JObject(
                    new JProperty("type", "Feature"),
                    new JProperty("geometry", new JObject(
                        new JProperty("type", "Point"),
                        new JProperty("coordinates", CoordinatesAsJSON))))));
                return result;
            }
        }
    }

    public class HistoricalLocation : SensorThingEntity
    {
        public DateTime Time { get; set; }
        public List<Location> Locations { get; } = new List<Location>();
        public Thing Thing { get; set; } = null;

        public HistoricalLocation() : base()
        {
            EntitySetName = "HistoricalLocations"; 
            NavigationLinks.Add("Locations");
            NavigationLinks.Add("Thing");
        }
         
        private void SetId() 
        {
            unchecked // Overflow is fine, just wrap
            {
                int hash = 17;
                if (Thing != null)
                {
                    hash = hash * 23 + Thing.Id;
                } 
                if (Locations.Any()) 
                {
                    hash = hash * 23 + Locations[0].Id;
                }
                Id = hash;
            }
        }

        public override JObject AsJSON
        {
            get
            {
                SetId();
                var result = base.AsJSON;
                result.Add(new JProperty("time",Time.ToString("o")));
                return result;
            }
        }
    }

    public class Datastream : NamedSensorThingEntity
    {
        public UnitOfMeasurement UnitOfMeasurement { get; } = new UnitOfMeasurement();
        public string ObservationType { get; } = ValueCodes.OM_Measurement;
        public BoundingBox ObservedArea { get; } = null;
        public TimeInterval PhenomenonTime { get; } = new TimeInterval();
        public TimeInterval ResultTime { get; } = new TimeInterval();
        public Thing Thing { get; set; } = null;
        //public Sensor Sensor { get; set; } = null;
        //public ObservedProperty ObservedProperty { get; set; } = null;
        //public List<Observation> Observations { get; } = new List<Observation>();
        
        public Datastream() : base()
        {
            EntitySetName = "Datastreams";
            NavigationLinks.Add("Thing");
            NavigationLinks.Add("Sensor");
            NavigationLinks.Add("ObservedProperty");
            NavigationLinks.Add("Observations");
        }

        public override JObject AsJSON
        {
            get
            {
                var result = base.AsJSON;
                if (UnitOfMeasurement != null)
                {
                    result.Add(new JProperty("unitOfMeasurement", new JObject(
                        new JProperty("name",UnitOfMeasurement.Name),
                        new JProperty("symbol",UnitOfMeasurement.Symbol),
                        new JProperty("definition",UnitOfMeasurement.Definition))));
                }
                result.Add(new JProperty("observationType", ObservationType));
                if (ObservedArea != null)
                {
                    result.Add(new JProperty("observedArea", new JObject(
                        new JProperty("type", "Polygon"),
                        new JProperty("coordinates", ObservedArea))));
                }
                result.Add(new JProperty("phenomenonTime", PhenomenonTime));
                result.Add(new JProperty("resultTime", ResultTime));
                return result;
            }
        }
    }
}

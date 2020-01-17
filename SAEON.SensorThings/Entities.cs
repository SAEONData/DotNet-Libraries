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
        public static readonly string SensorML = "http://www.opengis.net/doc/IS/SensorML/2.0";
        public static readonly string OM_Measurement = "http://www.opengis.net/def/observationType/OGC-OM/2.0/OM_Measurement";
    }

    public class Coordinate
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public double? Elevation { get; set; }

        public Coordinate() { }
        public Coordinate(double latitude, double longitude, double? elevation)
        {
            Latitude = latitude;
            Longitude = longitude;
            Elevation = elevation;
        }

        [JsonIgnore]
        public JObject AsJSON
        {
            get
            {
                var coords = new JArray(new JValue(Longitude), new JValue(Latitude));
                if (Elevation.HasValue) coords.Add(new JValue(Elevation.Value));
                return new JObject(
                    new JProperty("type", "Feature"),
                    new JProperty("geometry", new JObject(
                        new JProperty("type", "Point"),
                        new JProperty("coordinates", coords))));
            }
        }
    }

    public class UnitOfMeasurement
    {
        public string Name { get; set; }
        public string Symbol { get; set; }
        public string Definition { get; set; }

        [JsonIgnore]
        public JObject AsJSON
        {
            get
            {
                return new JObject(
                    new JProperty("name", Name),
                    new JProperty("symbol", Symbol),
                    new JProperty("definition", Definition));
            }
        }
    }

    public class BoundingBox
    {
        public double Left { get; set; }
        public double Bottom { get; set; }
        public double Right { get; set; }
        public double Top { get; set; }

        [JsonIgnore]
        public JObject AsJSON
        {
            get
            {
                var polygon = new JArray(
                                new JArray(new JValue(Left), new JValue(Top)),
                                new JArray(new JValue(Right), new JValue(Top)),
                                new JArray(new JValue(Right), new JValue(Bottom)),
                                new JArray(new JValue(Left), new JValue(Bottom)),
                                new JArray(new JValue(Left), new JValue(Top)));
                var exterior = new JArray
                {
                    polygon
                };
                return new JObject(
                    new JProperty("type", "Polygon"),
                    new JProperty("coordinates", exterior));
            }
        }
    }

    public class Time
    {
        public DateTime Value { get; set; }

        [JsonIgnore]
        public JValue AsJSON
        {
            get
            {
                return new JValue(Value.ToString("o"));
            }
        }
    }

    public class TimeInterval
    {
        [Required]
        public DateTime Start { get; set; }
        [Required]
        public DateTime End { get; set; }

        public TimeInterval()
        {
        }

        public TimeInterval(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        [JsonIgnore]
        public JValue AsJSON
        {
            get
            {
                return new JValue($"{Start.ToString("o")}/{End.ToString("o")}");
            }
        }
    }

    public class TimeOrInterval
    {
        [Required]
        public DateTime Start { get; set; }
        public DateTime? End { get; set; }

        [JsonIgnore]
        public JValue AsJSON
        {
            get
            {
                var result = $"{Start.ToString("o")}";
                if (End.HasValue)
                {
                    result += $"/{End.Value.ToString("o")}";
                }
                return new JValue(result);
            }
        }
    }

    public static class SensorThingsConfig
    {
        public static string BaseUrl { get; set; }
    }

    [Obsolete]
    public abstract class SensorThingEntity
    {
        public string EntitySetName { get; protected set; }

        public Guid Id { get; set; }
        public string SelfLink { get { return $"{SensorThingsConfig.BaseUrl}/{EntitySetName}({Id})"; } set {; } }
        public List<string> NavigationLinks { get; } = new List<string>();

        [JsonIgnore]
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
                    result.Add($"{link}@iot.navigationLink", $"{SensorThingsConfig.BaseUrl}/{EntitySetName}({Id})/{link}");
                }
                return result;
            }
        }

        //public object Call { get; private set; }
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
        public Dictionary<string, object> Properties { get; } = new Dictionary<string, object>();
        public Location Location { get; set; } = null;
        public List<HistoricalLocation> HistoricalLocations { get; } = new List<HistoricalLocation>();
        public List<Datastream> Datastreams { get; } = new List<Datastream>();

        public Thing() : base()
        {
            EntitySetName = "Things";
            NavigationLinks.Add("Location");
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
        public Coordinate Coordinate { get; set; } = null;
        public List<Thing> Things { get; } = new List<Thing>();
        public List<HistoricalLocation> HistoricalLocations { get; } = new List<HistoricalLocation>();

        public Location() : base()
        {
            EntitySetName = "Locations";
            NavigationLinks.Add("Things");
            NavigationLinks.Add("HistoricalLocations");
        }

        public override JObject AsJSON
        {
            get
            {
                var result = base.AsJSON;
                result.Add(new JProperty("encodingType", ValueCodes.GeoJson));
                result.Add(new JProperty("location", Coordinate?.AsJSON));
                return result;
            }
        }
    }

    public class HistoricalLocation : SensorThingEntity
    {
        public Time Time { get; } = new Time();
        public List<Location> Locations { get; } = new List<Location>();
        public Thing Thing { get; set; } = null;

        public HistoricalLocation() : base()
        {
            EntitySetName = "HistoricalLocations";
            NavigationLinks.Add("Locations");
            NavigationLinks.Add("Thing");
        }

        public HistoricalLocation(DateTime? time) : this()
        {
            Time.Value = time ?? DateTime.Now;
        }

        /*
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
        */

        public override JObject AsJSON
        {
            get
            {
                //SetId();
                var result = base.AsJSON;
                result.Add(new JProperty("time", Time.AsJSON));
                return result;
            }
        }
    }

    public class Datastream : NamedSensorThingEntity
    {
        public UnitOfMeasurement UnitOfMeasurement { get; } = new UnitOfMeasurement();
        public BoundingBox ObservedArea { get; set; } = null;
        public TimeInterval PhenomenonTime { get; set; } = null;
        public TimeInterval ResultTime { get; set; } = null;
        public Thing Thing { get; set; } = null;
        public Sensor Sensor { get; set; } = null;
        public ObservedProperty ObservedProperty { get; set; } = null;
        public List<Observation> Observations { get; } = new List<Observation>();

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
                    result.Add(new JProperty("unitOfMeasurement", UnitOfMeasurement.AsJSON));
                }
                result.Add(new JProperty("observationType", ValueCodes.OM_Measurement));
                if (ObservedArea != null)
                {
                    result.Add(new JProperty("observedArea", ObservedArea.AsJSON));
                }
                if (PhenomenonTime != null)
                {
                    result.Add(new JProperty("phenomenonTime", PhenomenonTime.AsJSON));
                }
                if (ResultTime != null)
                {
                    result.Add(new JProperty("resultTime", ResultTime.AsJSON));
                }
                return result;
            }
        }
    }

    public class Sensor : NamedSensorThingEntity
    {
        public string Metadata { get; set; }
        public Datastream Datastream { get; set; } = null;

        public Sensor() : base()
        {
            EntitySetName = "Sensors";
            NavigationLinks.Add("Datastream");
        }

        public override JObject AsJSON
        {
            get
            {
                var result = base.AsJSON;
                result.Add(new JProperty("encodingType", ValueCodes.Pdf));
                result.Add(new JProperty("metadata", Metadata));
                return result;
            }
        }
    }

    public class ObservedProperty : NamedSensorThingEntity
    {
        public string Definition { get; set; }
        public Datastream Datastream { get; set; } = null;

        public ObservedProperty() : base()
        {
            EntitySetName = "ObservedProperties";
            NavigationLinks.Add("Datastream");
        }

        public override JObject AsJSON
        {
            get
            {
                var result = base.AsJSON;
                result.Add(new JProperty("definition", Definition));
                return result;
            }
        }
    }

    public class Observation : SensorThingEntity
    {
        public TimeOrInterval PhenomenonTime { get; set; } = null;
        public double? Result { get; set; } = null;
        public Time ResultTime { get; set; } = null;
        public string Quality { get; set; }
        public TimeInterval ValidTime { get; set; } = null;
        public Dictionary<string, object> Parameters { get; } = new Dictionary<string, object>();
        public Datastream Datastream { get; set; } = null;
        public FeatureOfInterest FeatureOfInterest { get; set; } = null;


        public Observation() : base()
        {
            EntitySetName = "Observations";
            NavigationLinks.Add("Datastream");
            NavigationLinks.Add("FeatureOfInterest");
        }

        public override JObject AsJSON
        {
            get
            {
                var result = base.AsJSON;
                if (PhenomenonTime != null)
                    result.Add(new JProperty("phenomenonTime", PhenomenonTime.AsJSON));
                if (ResultTime != null)
                    result.Add(new JProperty("resultTime", ResultTime.AsJSON));
                result.Add(new JProperty("result", result));
                if (!string.IsNullOrEmpty(Quality))
                    result.Add(new JProperty("resultQuality", Quality));
                if (ValidTime != null)
                    result.Add(new JProperty("validTime", ValidTime.AsJSON));
                if (Parameters.Any())
                {
                    var property = new JObject();
                    foreach (var parameter in Parameters)
                    {
                        property.Add(new JProperty(parameter.Key, parameter.Value));
                    }
                    result.Add(new JProperty("parameters", property));
                }
                return result;
            }
        }
    }

    public class FeatureOfInterest : NamedSensorThingEntity
    {
        public Coordinate Coordinate { get; set; } = null;
        public double Longitude { get; set; }
        public double Latitude { get; set; }
        public double? Elevation { get; set; } = null;
        public List<Observation> Observations { get; } = new List<Observation>();

        public FeatureOfInterest() : base()
        {
            EntitySetName = "FeaturesOfInterest";
            NavigationLinks.Add("Observations");
        }

        public override JObject AsJSON
        {
            get
            {
                var result = base.AsJSON;
                result.Add(new JProperty("encodingType", ValueCodes.GeoJson));
                result.Add(new JProperty("location", Coordinate?.AsJSON));
                return result;
            }
        }

    }
}

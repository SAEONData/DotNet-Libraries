using Newtonsoft.Json.Linq;
using SAEON.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace SAEON.SensorThings
{
    public class SensorThingsApiController<TEntity> : ApiController where TEntity : SensorThingEntity
    {

        protected virtual List<TEntity> GetList()
        {
            return new List<TEntity>();
        }

        protected virtual JToken GetAllAsJSON() 
        {
            using (Logging.MethodCall<TEntity>(GetType()))
            {
                try
                {
                    var entityList = GetList();
                    Logging.Information("List: {count} {@list}", entityList.Count, entityList);
                    var resultList = new List<JToken>();
                    resultList.AddRange(entityList.Select(i => i.AsJSON));
                    var result = new JObject
                    {
                        new JProperty("@iot.count", entityList.Count),
                        new JProperty("value", resultList)
                    };
                    return result; 
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

    }
}


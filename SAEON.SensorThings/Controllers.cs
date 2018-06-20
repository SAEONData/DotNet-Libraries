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

        protected virtual List<TEntity> GetEntities()
        {
            return new List<TEntity>();
        }

        protected virtual TEntity GetEntity(int id)
        {
            throw new NotImplementedException();
        }

        [HttpGet]
        [Route]
        public virtual JToken GetAll()
        {
            using (Logging.MethodCall<TEntity>(GetType()))
            {
                try
                {
                    var entityList = GetEntities();
                    Logging.Verbose("List: {count} {@list}", entityList.Count, entityList);
                    var result = new JObject
                    {
                        new JProperty("@iot.count", entityList.Count),
                        new JProperty("value", entityList.Select(i => i.AsJSON))
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

        [HttpGet]
        [Route("{id:int}")]
        public virtual JToken GetById([FromUri]int id)
        {
            using (Logging.MethodCall<TEntity>(GetType()))
            {
                try
                {
                    return GetEntity(id).AsJSON;
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


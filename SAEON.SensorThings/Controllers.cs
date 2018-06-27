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
            return null;
        }

        protected JToken GetMany<TRelatedEntity>(int id, Func<int, List<TRelatedEntity>> getRelatedEntities) where TRelatedEntity : SensorThingEntity
        {
            using (Logging.MethodCall<TEntity, TRelatedEntity>(GetType()))
            {
                try
                {
                    var entities = getRelatedEntities(id);
                    Logging.Verbose("List: {count} {@list}", entities.Count, entities);
                    var result = new JObject
                    {
                        new JProperty("@iot.count", entities.Count),
                        new JProperty("value", entities.Select(i => i.AsJSON))
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
        [Route]
        public virtual JToken GetAll()
        {
            using (Logging.MethodCall<TEntity>(GetType()))
            {
                try
                {
                    var entities = GetEntities();
                    Logging.Verbose("List: {count} {@list}", entities.Count, entities);
                    var result = new JObject
                    {
                        new JProperty("@iot.count", entities.Count),
                        new JProperty("value", entities.Select(i => i.AsJSON))
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
        //[Route("({id:int})")]
        public virtual JToken GetById([FromUri]int id)
        {
            using (Logging.MethodCall<TEntity>(GetType()))
            {
                try
                {
                    var entity = GetEntity(id);
                    if (entity == null)
                        return null;
                    else
                        return entity.AsJSON;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }
         

        //[HttpGet]
        ////[Route("({id:int})/Related")]
        //public virtual JToken GetSingle<TRelatedEntity>([FromUri]int id) where TRelatedEntity : SensorThingEntity
        //{
        //    using (Logging.MethodCall<TEntity>(GetType()))
        //    {
        //        try
        //        {
        //            return GetRelatedEntity<TRelatedEntity>(id)?.AsJSON;
        //        }
        //        catch (Exception ex)
        //        {
        //            Logging.Exception(ex);
        //            throw;
        //        }
        //    }
        //}
    }
}


using AutoQueryable.AspNet.Filter.FilterAttributes;
using AutoQueryable.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SAEON.Logs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web.Http;

namespace SAEON.SensorThings
{
    [Obsolete]
    public abstract class SensorThingsApiController<TEntity> : ApiController where TEntity : SensorThingEntity
    {
        protected List<TEntity> Entities { get; private set; } = new List<TEntity>();

        protected void SetBaseUrl()
        {
            SensorThingsConfig.BaseUrl = Request.RequestUri.GetLeftPart(UriPartial.Authority) + "/SensorThings";
        }

        //[HttpGet, AutoQueryable(UseBaseType = true, DefaultToTake = 100)]
        //[Route("Entities")]
        //public virtual IQueryable<TEntity> GetEntities()
        //{
        //    using (Logging.MethodCall<TEntity>(GetType()))
        //    {
        //        try
        //        {
        //            SetBaseUrl();
        //            var queryString = Request.RequestUri.Query;
        //            Logging.Verbose("QueryString: {QueryString}", queryString);
        //            return Entities.AsQueryable();
        //        }
        //        catch (Exception ex)
        //        {
        //            Logging.Exception(ex);
        //            throw;
        //        }
        //    }
        //}

        [HttpGet]
        [Route]
        public virtual JToken GetAll()
        {
            using (Logging.MethodCall<TEntity>(GetType()))
            {
                try
                {
                    SetBaseUrl();
                    var queryString = Request.RequestUri.Query.Replace("$", "");
                    Logging.Verbose("QueryString: {QueryString}", queryString);
                    //var entities = Entities.AsQueryable().AutoQueryable(queryString);
                    //string json = JsonConvert.SerializeObject(entities);
                    ////Logging.Verbose("json: {json}", json);
                    //var arr = JArray.Parse(json);
                    //Logging.Verbose("List: {count} {@list}", arr.Count(), arr.ToString());
                    //var result = new JObject
                    //{
                    //    new JProperty("@iot.count", arr.Count()),
                    //    new JProperty("value", arr)
                    //};
                    //return result;
                    var entities = Entities;
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
        //[Route("Entity({id:guid})")]
        public virtual JToken GetById([FromUri]Guid id)
        {
            using (Logging.MethodCall<TEntity>(GetType()))
            {
                try
                {
                    SetBaseUrl();
                    var entity = Entities.FirstOrDefault(i => i.Id == id);
                    if (entity == null)
                    {
                        return null;
                    }
                    else
                    {
                        return entity.AsJSON;
                    }
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        [HttpGet]
        //[Route("Entity({id:int})/Related")]
        public virtual JToken GetSingle<TRelated>([FromUri]Guid id, Expression<Func<TEntity, TRelated>> select) where TRelated : SensorThingEntity
        {
            using (Logging.MethodCall<TEntity, TRelated>(GetType()))
            {
                try
                {
                    SetBaseUrl();
                    if (!Entities.Any(i => i.Id == id))
                    {
                        return null;
                    }

                    TRelated related = Entities.AsQueryable().Where(i => i.Id == id).Select(select).FirstOrDefault();
                    if (related == null)
                    {
                        return null;
                    }

                    Logging.Verbose("Related: {@Related}", related);
                    return related.AsJSON;
                }
                catch (Exception ex)
                {
                    Logging.Exception(ex);
                    throw;
                }
            }
        }

        [HttpGet]
        //[Route("Entity({id:int})/Related")]
        public virtual JToken GetMany<TRelated>([FromUri]Guid id, Expression<Func<TEntity, IEnumerable<TRelated>>> select) where TRelated : SensorThingEntity
        {
            using (Logging.MethodCall<TEntity, TRelated>(GetType()))
            {
                try
                {
                    SetBaseUrl();
                    if (!Entities.Any(i => i.Id == id))
                    {
                        return null;
                    }

                    List<TRelated> related = Entities.AsQueryable().Where(i => i.Id == id).SelectMany(select).ToList();
                    Logging.Verbose("Related: {count} {@Related}", related.Count, related);
                    var result = new JObject
                    {
                        new JProperty("@iot.count", related.Count),
                        new JProperty("value", related.Select(i => i.AsJSON))
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


using System;
using System.Web;
using System.Web.Mvc;

namespace SAEON.AspNet.Mvc
{
    public sealed class NoCacheIfLocalAttribute : ActionFilterAttribute
    {
        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException(nameof(filterContext));
            if (filterContext.HttpContext.Request.IsLocal)
            {
                filterContext.HttpContext.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            }
            base.OnResultExecuted(filterContext);
        }
    }
}

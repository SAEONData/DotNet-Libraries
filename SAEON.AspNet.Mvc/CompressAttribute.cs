using SAEON.AspNet.Common;
using System;
using System.IO.Compression;
using System.Web;
using System.Web.Mvc;

namespace SAEON.AspNet.Mvc
{
    public sealed class CompressAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (filterContext == null) throw new ArgumentNullException(nameof(filterContext));
            HttpRequestBase request = filterContext.HttpContext.Request;

            string acceptEncoding = request.Headers[AspNetConstants.AcceptEncoding];

            if (string.IsNullOrEmpty(acceptEncoding)) return;

            acceptEncoding = acceptEncoding.ToUpperInvariant();

            HttpResponseBase response = filterContext.HttpContext.Response;

            if (acceptEncoding.Contains("GZIP"))
            {
                response.AppendHeader(AspNetConstants.ContentEncoding, "gzip");
                response.Filter = new GZipStream(response.Filter, CompressionMode.Compress);
            }
            else if (acceptEncoding.Contains("DEFLATE"))
            {
                response.AppendHeader(AspNetConstants.ContentEncoding, "deflate");
                response.Filter = new DeflateStream(response.Filter, CompressionMode.Compress);
            }
        }
    }
}

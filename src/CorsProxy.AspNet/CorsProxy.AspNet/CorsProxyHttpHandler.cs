using System;
using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace CorsProxy.AspNet
{
    /// <summary>
    ///     Proxies Ajax requests over your front end web server.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Will copy most headers from the Ajax request to the request that is sent to the proxied server. You can
    ///         therefore use cookies, custom headers etc.
    ///     </para>
    ///     <para>
    ///         Uses the HTTP header <c>X-CorsProxy-Url</c> to identify which server to send the proxy request to.
    ///     </para>
    ///     <para>
    ///         Adds the <code>X-CorsProxy-Failure</code> header to indicate whether non 2xx responses is due
    ///         to this library or the destination web server.
    ///     </para>
    /// </remarks>
    public class CorsProxyHttpHandler : IHttpHandler
    {
        private const string NO_PROXY_URL = "X-CorsProxy-Url was not specified. The CorsProxy should only be invoked from the proxy JavaScript.";
        private const string FORBIDDEN_URL = "X-CorsProxy is not allowed to route to '{0}'";

        /// <summary>
        /// Checks Request to ensure we have a TargetUrl and TargetUrl is not forbidden
        /// </summary>
        /// <param name="context">Request Context</param>
        /// <param name="url">Requested Url</param>
        /// <returns>Whether we have a valid request or not</returns>
        bool EnsureRequestIsValid(HttpContext context, string url)
        {
            bool requestIsValid = true;
            if (string.IsNullOrEmpty(url))
            {
                context.Response.StatusCode = 501;
                context.Response.StatusDescription = NO_PROXY_URL;
                context.Response.End();
                requestIsValid = false;
            }

            var isForbidden = context.Items["CorsProxy-Forbidden"] as bool?;
            if (isForbidden == true)
            {
                context.Response.StatusCode = 403;
                context.Response.StatusDescription = string.Format(FORBIDDEN_URL, url);
                context.Response.End();
                requestIsValid = false;
            }

            return requestIsValid;
        }

        /// <summary>
        ///     Enables processing of HTTP Web requests by a custom HttpHandler that implements the
        ///     <see cref="T:System.Web.IHttpHandler" /> interface.
        /// </summary>
        /// <param name="context">
        ///     An <see cref="T:System.Web.HttpContext" /> object that provides references to the intrinsic
        ///     server objects (for example, Request, Response, Session, and Server) used to service HTTP requests.
        /// </param>
        public void ProcessRequest(HttpContext context)
        {
            var url = context.Request.Headers["X-CorsProxy-Url"];
            if (!EnsureRequestIsValid(context, url))
                return;

            HttpWebResponse response;
            HttpWebRequest outgoing = (HttpWebRequest)WebRequest.Create(url);
            var connectionTimeout = context.Items["CorsProxy-Timeout"] as int?;
            if (connectionTimeout.HasValue && connectionTimeout > 0)
            {
                outgoing.Timeout = connectionTimeout.Value;
            }
            Utility.CopyHeaders(context.Request, outgoing);
            CookieContainer cookieJar = new CookieContainer();
            Utility.CopyCookies(context.Request, cookieJar, url);//Remote
            outgoing.CookieContainer = cookieJar;

            // Copy Data
            if (!context.Request.HttpMethod.Equals("GET", StringComparison.OrdinalIgnoreCase))
            {
                outgoing.ContentLength = context.Request.ContentLength;
                Utility.CopyStream(context.Request.InputStream, outgoing.GetRequestStream());
            }

            try
            {
                response = (HttpWebResponse)outgoing.GetResponse();
            }
            catch (WebException ex)
            {
                Utility.Return500(context, ex);
                return;
            }

            Stream receiveStream = response.GetResponseStream();
            Utility.CopyCookies(response, context.Response, context.Request.Url.Host);//Local Proxy
            context.Response.ContentType = response.ContentType;
            Utility.CopyStream(receiveStream, context.Response.OutputStream);
            response.Close();
            context.Response.End();
        }

        /// <summary>
        ///     Gets a value indicating whether another request can use the <see cref="T:System.Web.IHttpHandler" /> instance.
        /// </summary>
        /// <returns>
        ///     true if the <see cref="T:System.Web.IHttpHandler" /> instance is reusable; otherwise, false.
        /// </returns>
        public bool IsReusable
        {
            get { return true; }
        }
    }
}
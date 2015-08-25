﻿﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Text;
using System.IO;
using System.Net;

namespace CorsProxy.AspNet
{
    /// <summary>
    /// See: https://code.google.com/p/reverseproxy/source/browse/trunk/Utility.cs
    /// </summary>
    public class Utility
    {
        public const int BUFFER_LENGTH = 1024;

        public static string ConvertStream(Stream stream)
        {
            byte[] buff = new byte[BUFFER_LENGTH];
            int bytes = 0;
            StringBuilder sb = new StringBuilder();
            while ((bytes = stream.Read(buff, 0, BUFFER_LENGTH)) > 0)
            {
                System.Text.ASCIIEncoding enc = new System.Text.ASCIIEncoding();
                sb.Append(enc.GetString(buff, 0, bytes));
            }
            return sb.ToString();
        }

        public static void CopyCookies(HttpWebResponse from, HttpResponse to, string host)
        {
            to.Cookies.Clear();

            foreach (Cookie receivedCookie in from.Cookies)
            {
                HttpCookie c = new HttpCookie(receivedCookie.Name,
                                   receivedCookie.Value);
                c.Domain = host;
                c.Expires = receivedCookie.Expires;
                c.HttpOnly = receivedCookie.HttpOnly;
                c.Path = receivedCookie.Path;
                c.Secure = receivedCookie.Secure;
                to.Cookies.Add(c);
            }
        }

        public static void CopyHeaders(HttpRequest from, HttpWebRequest to)
        {
            string value;
            foreach (string key in from.Headers.AllKeys)
            {
                value = from.Headers[key];
                switch (key)
                {
                    case "Host":
                    case "Connection":
                    case "Content-Length":
                    case "X-AspNet-Version":
                    case "X-CorsProxy-Url":
                        //Ignore, will be populated as needed by the outgoing request
                        break;
                    case "Expect":
                        /* Not sure how to impliment this one. Just filtering it out for now.
                         * if (value == "100-Continue")
                            System.Net.ServicePointManager.Expect100Continue=true;
                         * */
                        break;
                    case "Content-Type":
                        to.ContentType = value;
                        break;
                    case "Accept":
                        to.Accept = value;
                        break;
                    case "Referer":
                        to.Referer = value;
                        break;
                    case "User-Agent":
                        to.UserAgent = value;
                        break;
                    case "Accept-Encoding":
                        /*  Filter out, because I'm not sure how to impliment that correctly  */
                        break;
                    default:
                        to.Headers.Add(key, value);
                        break;
                }
            }
            // Not a header per say... but close enough
            to.Method = from.RequestType;
        }

        public static void Return404(HttpContext context)
        {
            context.Response.StatusCode = 404;
            context.Response.StatusDescription = "Not Found";
            context.Response.Write("<h2>Not Found</h2>");
            context.Response.End();
        }

        public static void Return500(HttpContext context, WebException ex)
        {
            context.Response.StatusCode = 500;
            context.Response.StatusDescription = "Proxy Error";
            context.Response.Write("<h2>Error connecting to upstream server</h2>");
            context.Response.Write("<pre>");

            CopyStream(ex.Response.GetResponseStream(), context.Response.OutputStream);

            context.Response.Write("</pre>");
            context.Response.End();
        }

        /// <summary>
        /// Copy data from one stream, to the other.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dest"></param>
        public static void CopyStream(Stream source, Stream dest)
        {
            byte[] buffer = new byte[BUFFER_LENGTH];
            int readBytes;
            while ((readBytes = source.Read(buffer, 0, BUFFER_LENGTH)) > 0)
            {
                dest.Write(buffer, 0, readBytes);
            }
        }
    }
}

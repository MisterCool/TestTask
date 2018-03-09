using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Text.RegularExpressions;

namespace ValidationRequest
{
    public class RequestInfoValidationAttribute : ValidationAttribute
    {
        public override bool IsValid(object value)
        {
            RequestInfo info = value as RequestInfo;

            return ValidCoords(info.Coords) && ValidTransform(info.Transform) && OtherChecks(info);
        }
        private static bool ValidCoords(string inputString)
        {
            var pattern = @"^(-?\d+),(-?\d+),(-?\d+),(-?\d+)\z";

            return Regex.IsMatch(inputString, pattern);
        }
        private static bool ValidTransform(string inputString)
        {
            var allTransform = new string[] { "rotate-cw", "rotate-ccw", "flip-v", "flip-h" };

            return allTransform.Contains(inputString);
        }
        private static bool OtherChecks(RequestInfo info)
        {
            var maxBodySize = 100000;
            var maxHeight = 1000;
            var maxWidth = 1000;
            return info.ContentLength < maxBodySize && info.RequestBody.Height < maxHeight && 
                info.RequestBody.Width < maxWidth && info.HttpMethod == "POST";
        }
    }
    [RequestInfoValidation]
    public class RequestInfo
    {
        public readonly string Transform;
        public readonly string Coords;
        public readonly string HttpMethod;
        public readonly long ContentLength;
        public readonly Bitmap RequestBody;
        public readonly string ContentType;
        public RequestInfo(HttpListenerRequest request)
        {
            var url = new Uri(request.Url.ToString());
            Transform = url.Segments[url.Segments.Count() - 2].Remove(url.Segments[url.Segments.Count() - 2].Length - 1, 1);
            RequestBody = new Bitmap(request.InputStream);
            Coords = url.Segments.Last();
            ContentType = request.ContentType;
            ContentLength = request.ContentLength64;
            HttpMethod = request.HttpMethod;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CheckRequest
{
    public class ValidationRequest
    {
        public static bool CheckCorrectnessOfTheRequest(HttpListenerRequest request)
        {
            var url = new Uri(request.Url.ToString());
            var array = url.Segments;
            var coordsString = url.Segments.Last();
            var filter = url.Segments[url.Segments.Count() - 2].Remove(url.Segments[url.Segments.Count() - 2].Length - 1, 1);
            return request.HttpMethod == "POST" && request.ContentType == "application/octet-stream" && 
                CheckCorrectnessCoords(coordsString) && CheckValidFilter(filter) && request.ContentLength64 < 100000;
        }
        public static bool CheckCorrectnessCoords(string inputString)
        {
            var flag = true;
            var strSplit = inputString.Split(',');
            if (strSplit.Count() != 4)
                flag = false;

            foreach (var e in strSplit)
                if (Math.Abs(int.Parse(e)) > Math.Pow(2, 31))
                    flag = false;
            return flag;
        }
        public static bool CheckValidFilter(string inputString)
        {
            var flag = false;
            var arr = inputString.Split('(', ')');
            if (arr[0] == "threshold" && arr[2] == String.Empty)
            {
                if (int.TryParse(arr[1], out int number))
                    if (int.Parse(arr[1]) >= 0 && int.Parse(arr[1]) <= 100)
                        flag = true;
            }
            else if (inputString == "grayscale" || inputString == "sepia")
                flag = true;
            return flag;
        }
    }
}

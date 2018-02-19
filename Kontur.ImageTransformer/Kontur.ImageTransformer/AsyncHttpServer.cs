using CheckRequest;
using FilterForImage;
using System;
using System.Collections.Concurrent;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
namespace Kontur.ImageTransformer
{
    internal class AsyncHttpServer : IDisposable
    {
        public AsyncHttpServer()
        {
            listener = new HttpListener();
        }

        public void Start(string prefix)
        {
            lock (listener)
            {
                if (!isRunning)
                {
                    listener.Prefixes.Clear();
                    listener.Prefixes.Add(prefix);
                    listener.Start();

                    listenerThread = new Thread(Listen)
                    {
                        IsBackground = true,
                        Priority = ThreadPriority.Highest
                    };
                    listenerThread.Start();
                    DelContext del = HandleContextAsync;
                    QueueHandler(del, queueContext);
                    isRunning = true;
                }
            }
        }

        public void Stop()
        {
            lock (listener)
            {
                if (!isRunning)
                    return;

                listener.Stop();

                listenerThread.Abort();
                listenerThread.Join();

                isRunning = false;
            }
        }

        public void Dispose()
        {
            if (disposed)
                return;

            disposed = true;

            Stop();

            listener.Close();
        }
       
        private void Listen()
        {
            while (true)
            {
                try
                {
                    if (listener.IsListening)
                    {
                        var context = listener.GetContext();
                        var limitRequest = 50;
                        if (queueContext.Count() > limitRequest)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.ServiceUnavailable;
                            context.Response.OutputStream.Close();
                            context.Response.Close();
                        }
                        else
                        {
                            queueContext.Enqueue(context);
                        }
                    }
                    else Thread.Sleep(0);

                }
                catch (ThreadAbortException)
                {
                    return;
                }
                //catch (Exception error)
                //{
                //    // TODO: log errors
                //}
            }
        }
        private static void QueueHandler(DelContext del, ConcurrentQueue<HttpListenerContext> queueContext)
        {
            Task.Run(() =>
            {
                while (true)
                {
                    if (countTask < limitTask && queueContext.TryDequeue(out HttpListenerContext context))
                    {
                        Interlocked.Increment(ref countTask);
                        Task.Run(() =>
                        {
                            var task = del.Invoke(context);
                            var timeOfProcessing = TimeSpan.FromMilliseconds(1000);
                            if (!task.Wait(timeOfProcessing))
                            {
                                context.Response.StatusCode = 429;
                                context.Response.OutputStream.Close();
                                context.Response.Close();
                            }

                        }).ContinueWith(t => Interlocked.Decrement(ref countTask));
                    }
                }
            });
        }
        private Task HandleContextAsync(HttpListenerContext listenerContext)
        {
            return Task.Run(() =>
             {
                 var request = listenerContext.Request;
                 var imageBit = new Bitmap(request.InputStream);
                 if (!(ValidationRequest.CheckCorrectnessOfTheRequest(listenerContext.Request) && imageBit.RawFormat.Equals(ImageFormat.Png)))
                 {
                     listenerContext.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                     listenerContext.Response.OutputStream.Close();
                     listenerContext.Response.Close();
                 }
                 else
                 {
                     var url = new Uri(request.Url.ToString());
                     var array = url.Segments;
                     var coordsString = url.Segments.Last();
                     var filter = url.Segments[url.Segments.Count() - 2].Remove(url.Segments[url.Segments.Count() - 2].Length - 1, 1);

                     var coordsStringSplit = coordsString.Split(',');
                     var coords = new int[4];
                     for (var i = 0; i < coords.Length; i++)
                         coords[i] = int.Parse(coordsStringSplit[i]);

                     switch (filter)
                     {
                         case "sepia":
                             Filter.ApplyFilter(imageBit, filter);
                             break;
                         case "grayscale":
                             Filter.ApplyFilter(imageBit, filter);
                             break;
                         default:
                             var arr = filter.Split('(', ')');
                             var x = int.Parse(arr[1]);
                             Filter.ApplyFilter(imageBit, filter);
                             break;
                     }

                     var rectangleImage = new Rectangle(0, 0, imageBit.Width, imageBit.Height);
                     var rectangleCoords = new Rectangle(coords[0], coords[1], coords[2], coords[3]);

                     var imageInterSect = Rectangle.Intersect(rectangleImage, rectangleCoords);
                     if (imageInterSect == Rectangle.Empty)
                     {
                         listenerContext.Response.StatusCode = (int)HttpStatusCode.NoContent;
                         listenerContext.Response.OutputStream.Close();
                         listenerContext.Response.Close();
                     }
                     else
                     {
                         listenerContext.Response.StatusCode = (int)HttpStatusCode.OK;
                         listenerContext.Response.ContentType = "image/png";
                         var iBit = imageBit.Clone(imageInterSect, imageBit.PixelFormat);
                         iBit.Save(listenerContext.Response.OutputStream, ImageFormat.Png);
                         listenerContext.Response.OutputStream.Close();
                         listenerContext.Response.Close();
                     }
                 }
             });
        }
        private static int limitTask = 100;
        private delegate Task DelContext(HttpListenerContext context);
        private readonly HttpListener listener;
        private Thread listenerThread;
        private bool disposed;
        private volatile bool isRunning;
        private static int countTask = 0;
        private static ConcurrentQueue<HttpListenerContext> queueContext = new ConcurrentQueue<HttpListenerContext>();
    }
}

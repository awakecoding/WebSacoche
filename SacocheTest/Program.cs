using System;
using System.Net;
using System.Text;
using Netwrk.Web;

namespace SacocheTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var webListener = new NetwrkWebListener(12345);

            webListener.OnWebSocketConnection += (listener, socket) =>
                {
                    socket.OnBinaryMessage += (webSocket, data) =>
                    {
                        string txt = Encoding.UTF8.GetString(data);
                        Console.WriteLine("{0}: {1}", data.Length, txt);
                        Console.WriteLine("{0}", BitConverter.ToString(data));
                        webSocket.Send(data);
                    };
                };
            bool started = webListener.Start();

            Console.ReadLine();

        }
    }
}
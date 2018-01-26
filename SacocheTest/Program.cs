using System;
using System.Net;
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
                        webSocket.Send(data);
                    };
                };
            bool started = webListener.Start();

            Console.ReadLine();

        }
    }
}
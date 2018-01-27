using System;
using System.Net;
using System.Text;
using Sacoche;

namespace SacocheTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var webListener = new SacocheWebListener(12345);

            webListener.OnWebSocketConnection += (listener, socket) =>
                {
                    socket.OnBinaryMessage += (webSocket, data) =>
                    {
                        string txt = Encoding.UTF8.GetString(data);
                        Console.WriteLine("{0}: {1}", data.Length, txt);
                        webSocket.Send(data);
                    };

                    socket.OnClose += (webSocket) =>
                    {
                        Console.WriteLine("OnClose");
                    };
                };
            bool started = webListener.Start();

            Console.ReadLine();

        }
    }
}
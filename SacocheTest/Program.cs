using System;
using System.Net;
using System.Text;
using Sacoche;

namespace SacocheTest
{
    class Program
    {
        static int test_server()
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

                    socket.OnClose += (webSocket, clean, code, reason) =>
                    {
                        Console.WriteLine("OnClose: clean: {0} code: {1}", clean, code);
                    };
                };

            bool started = webListener.Start();
            return 1;
        }

        static int test_client()
        {
            var url = new Uri("ws://echo.websocket.org");
            var socket = new SacocheWebSocket();

            socket.OnBinaryMessage += (webSocket, data) =>
            {
                string txt = Encoding.UTF8.GetString(data);
                Console.WriteLine("{0}: {1}", data.Length, txt);
            };

            socket.OnTextMessage += (webSocket, data) =>
            {
                Console.WriteLine("{0}", data);
            };

            socket.OnClose += (webSocket, clean, code, reason) =>
            {
                Console.WriteLine("OnClose: clean: {0} code: {1}", clean, code);
            };

            bool success = socket.ConnectAsync(url).GetAwaiter().GetResult();
            Console.WriteLine("Connected: {0}", success);

            socket.Send(Encoding.UTF8.GetBytes("hello, it's me"));

            return 1;
        }
     
        static void Main(string[] args)
        {
            //test_client();
            test_server();
            Console.ReadLine();
        }
    }
}
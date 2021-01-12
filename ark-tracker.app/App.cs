using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace ark_tracker.app
{
    public static class App
    {
        static Trading212Handler _handler;
        private static AutoResetEvent _monitor = new AutoResetEvent(false);
        static void Main(string[] args)
        {
            var a = new[] { "" };

            new Thread(RunnerThread).Start();

            var exiting = false;
            while (!exiting)
            {
                var input = Console.ReadLine();
                switch (input?.ToLower())
                {
                    case "update":
                        _handler?.StartEdit();
                        break;
                    case "q": 
                        exiting = true;
                        _monitor.Set();
                        break;
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        
        private static void RunnerThread()
        {
            using(_handler = new Trading212Handler())
            {
                _handler.Start();
                _monitor.WaitOne();
            }
        }
    }
}

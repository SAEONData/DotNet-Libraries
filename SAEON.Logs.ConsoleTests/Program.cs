using System;
using System.Text;

namespace SAEON.Logs.ConsoleTests
{
    class Program
    {

        public class MyClass
        {
            public MyClass()
            {
                using (Logging.MethodCall(GetType()))
                {
                    Logging.Information("Constructor");
                }
            }

            ~MyClass()
            {
                using (Logging.MethodCall(GetType()))
                {
                    Logging.Information("Destructor");
                }
            }

            public void DoSomething()
            {
                using (Logging.MethodCall(GetType()))
                {
                    Logging.Information("Doing something");
                }
            }
        }

        public class MyGeneric<T>
        {
            public MyGeneric()
            {
                using (Logging.MethodCall<T>(GetType()))
                {
                    Logging.Information("Constructor");
                }
            }

            ~MyGeneric()
            {
                using (Logging.MethodCall<T>(GetType()))
                {
                    Logging.Information("Destructor");
                }
            }

            public void DoSomething()
            {
                using (Logging.MethodCall<T>(GetType()))
                {
                    Logging.Information("Doing something");
                }
            }

        }


        static void Main(string[] args)
        {
            try
            {
                Logging
                    .CreateConfiguration("Logs/SAEON.Observations.WebSite.Admin {Date}.txt")
                    .Create();
                Logging.Information("Start");
                var m = new MyClass();
                m.DoSomething();
                var g = new MyGeneric<int>();
                g.DoSomething();
                Logging.Information("Done");
            }
            finally
            {
                Logging.ShutDown();
            }
        }
    }
}

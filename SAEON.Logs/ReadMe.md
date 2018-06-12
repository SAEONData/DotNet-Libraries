# SAEON.Logs #
The South African Environmental Observation Network (SAEON) Logs provides logging for .Net Core, Standard and Full

## .Net Core
Program.cs
```
public static void Main(string[] args)
{
    Logging
        .CreateConfiguration((@"/Logs/AppName.txt"))
        .Create();
    var host = BuildWebHost(args);
    host.Run();
}

public static IWebHost BuildWebHost(string[] args) =>
    WebHost
        .CreateDefaultBuilder(args)
        .UseStartup<Startup>()
        .UseSAEONLogs()
        .Build();
```

## Full web app
Global.asmx.cs
```
protected void Application_Start()
{
    Logging
        .CreateConfiguration(HostingEnvironment.MapPath(@"~/App_Data/Logs/AppName {Date}.txt"))
        .Create();
}
```


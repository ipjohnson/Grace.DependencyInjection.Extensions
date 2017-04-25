# Extensions for using Grace in ASP.Net Core

Using Grace in an ASP.Net Core application is really simple, add the [Grace.AspNetCore.Hosting](https://www.nuget.org/packages/Grace.AspNetCore.Hosting) package. Then add the following to your project

Program.cs
```
var host = new WebHostBuilder()
                .UseKestrel()
                .UseContentRoot(Directory.GetCurrentDirectory())
                .UseIISIntegration()
                .UseGrace()  // add grace
                .UseStartup<Startup>()
                .Build();
```

Startup.cs
```
public void ConfigureServices(IServiceCollection services)
{
  services.AddMvc();
}

// add this method
public void ConfigureContainer(IInjectionScope scope)
{
  
}
```

You can also replace the Controller and View activators allowing for controllers and views to be created by the container allowing for more performance and features.

Add the [Grace.AspNetCore.MVC](https://www.nuget.org/packages/Grace.AspNetCore.MVC) nuget package.

Startup.cs
```
public void ConfigureContainer(IInjectionScope scope)
{
  scope.SetupMvc();
}
```

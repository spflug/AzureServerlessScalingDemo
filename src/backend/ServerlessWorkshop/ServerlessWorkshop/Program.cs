using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using ServerlessWorkshop;

var builder = Host
    .CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(b => { b.UseStartup<Startup>(); });

builder.Build().Run();
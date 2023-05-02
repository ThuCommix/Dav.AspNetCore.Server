using Dav.AspNetCore.Server;
using Dav.AspNetCore.Server.Authentication;
using Dav.AspNetCore.Server.Store;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = null;
});

/*builder.Services.AddAuthentication().AddBasic(options =>
{
    options.Events.OnAuthenticate = (context, cancellationToken) =>
    {
        if (context.UserName == "Demo" &&
            context.Password == "password")
        {
            return Task.FromResult(true);
        }

        return Task.FromResult(false);
    };
});*/

/*builder.Services.AddAuthentication().AddDigest(options =>
{
    options.Events.OnRequestPassword = (context, cancellationToken) =>
    {
        if (context.UserName == "Demo")
            return Task.FromResult<string?>("password");

        return Task.FromResult<string?>(null);
    };
});*/

builder.Services.AddWebDav(davBuilder =>
{
    // davBuilder.RequiresAuthentication = true;
    davBuilder.AddLocalFiles(options =>
    {
        options.RootPath = "/Users/kevin/Projects/tmp/";
    });
});

var app = builder.Build();

app.Map("/dav", davApp =>
{
    // davApp.UseAuthentication();
    davApp.UseWebDav();
});

app.Run();
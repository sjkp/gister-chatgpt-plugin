using Microsoft.OpenApi.Models;
using Octokit;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {            
            Type = SecuritySchemeType.ApiKey,
            Name = "Authorization",
            In = ParameterLocation.Header,
        }
    );

    options.AddSecurityRequirement(
        new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                new string[] { }
            }
        }
    );
});

builder.Services.AddCors(
    options => options.AddDefaultPolicy(corsPolicyBuilder => corsPolicyBuilder.WithOrigins("https://chat.openai.com").AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
app.UseCors();
// Configure the HTTP request pipeline.
app.UseSwagger();
app.UseSwaggerUI();




app.MapPost("creategist", async (string code, HttpRequest req) =>
{
    var client = new GitHubClient(new ProductHeaderValue("gister"));
    var token = req.Headers["Authorization"].ToString().Split(" ").Last();
    var gist = new NewGist()
    {
        Description = "Some description",
    };
    gist.Files.Add("file1.html", code);
    var creds = new Credentials(token, AuthenticationType.Bearer);

    client.Credentials = creds;

    var res = await client.Gist.Create(gist);
    return new
    {
        url = res.HtmlUrl,
        htmlPreviewUrl = $"https://htmlpreview.github.io/?{res.HtmlUrl}/raw/file1.html"
    };
})
.WithName("creategist")
.WithOpenApi(gen =>
{
    gen.Description = "Post your code as a gist to github";
    gen.Parameters[0].Description = "Code that should be stored as a gist";
    return gen;
});


app.MapGet(pattern: "/logo.png", () => Results.File(path: "logo.png", contentType: "image/png"))
   .WithName("PluginLogo")
   .ExcludeFromDescription();

app.MapGet(
        pattern: "/.well-known/ai-plugin.json",
        async context =>
        {
            string host = context.Request.Host.ToString();
            string text = await System.IO.File.ReadAllTextAsync("manifest.json");
            text = text.Replace(oldValue: "PLUGIN_HOSTNAME", $"{Environment.GetEnvironmentVariable("PLUGIN_HOSTNAME")}");
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(text);
        })
   .WithName("PluginManifest");




app.Run();

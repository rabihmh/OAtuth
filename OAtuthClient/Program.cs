
using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace OAtuthClient
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthentication("cookie")
                .AddCookie("cookie")
                .AddOAuth("github", o =>
                {
                    o.SignInScheme = "cookie";
                    o.ClientId = "9e801b8819a5f3af1664";
                    o.ClientSecret = "3a36f6a3ce31450144bb11012a066fd833b964b6";
                    o.AuthorizationEndpoint = "https://github.com/login/oauth/authorize";
                    o.TokenEndpoint = "https://github.com/login/oauth/access_token";
                    o.SaveTokens = true;
                    o.CallbackPath = "/oauth/github-cb";

                    o.UserInformationEndpoint = "https://api.github.com/user";

                    o.ClaimActions.MapJsonKey("sub", "id");
                    o.ClaimActions.MapJsonKey(ClaimTypes.Name, "login");



                    o.Events.OnCreatingTicket = async ctx =>
                    {
                       using var request = new HttpRequestMessage(HttpMethod.Get, ctx.Options.UserInformationEndpoint);
                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", ctx.AccessToken);
                        using var result=await ctx.Backchannel.SendAsync(request);
                        var user = await result.Content.ReadFromJsonAsync<JsonElement>();
                        ctx.RunClaimActions(user);
                    };
                });
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapGet("/", (HttpContext ctx) => {

                ctx.GetTokenAsync("access_token");
                return ctx.User.Claims.Select(x => new {x.Type, x.Value}).ToList();
            });
            app.MapGet("/login", (HttpContext ctx) => {

                return Results.Challenge(
                    new AuthenticationProperties()
                    {
                        RedirectUri= "https://localhost:7247/"
                    },
                    authenticationSchemes:new List<string>() { "github"}
                    );
            });

            app.Run();
        }
    }
}

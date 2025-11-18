

var builder = WebApplication.CreateBuilder(args);

// Authentication
//builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
//    .AddJwtBearer(options =>
//    {
//        options.Authority = "https://<keycloak-host>/realms/myrealm";
//        options.Audience = "my-dotnet-client";
//        options.RequireHttpsMetadata = true;

//        options.TokenValidationParameters = new TokenValidationParameters
//        {
//            ValidateAudience = true,
//            ValidateIssuer = true
//        };
//    });


builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();


app.MapReverseProxy();

app.Run();

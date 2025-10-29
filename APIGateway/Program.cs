
namespace APIGateway
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddReverseProxy()
                .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

            var app = builder.Build();

            //app.UseHttpsRedirection();

            //app.UseAuthorization();
            app.MapReverseProxy();

            app.Run();
        }
    }
}

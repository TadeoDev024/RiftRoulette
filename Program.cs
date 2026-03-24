using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace RiftRoulette
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // 1. Añadir Controladores
            builder.Services.AddControllers();

            // 2. Configurar CORS (Permitir todo para pruebas)
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowAll", policy =>
                {
                    policy.SetIsOriginAllowed(_ => true) // Permite localhost o Vercel
                          .AllowAnyHeader()
                          .AllowAnyMethod()
                          .AllowCredentials(); // Importante
                });
            });

            var app = builder.Build();

            // 3. PIPELINE DE EJECUCIÓN (EL ORDEN ES VITAL)
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            // CORS siempre va entre Routing y Endpoints/Controllers
            app.UseCors("AllowAll");

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
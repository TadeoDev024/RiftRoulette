using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. CONFIGURACIÓN DE SERVICIOS
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configuración de Swagger (Opcional para ver tu API online)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "RiftRoulette API", Version = "v1" });
});

// 2. CONFIGURACIÓN DE CORS (CRÍTICO para conectar con Vercel)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercel",
        policy =>
        {
            policy.WithOrigins("https://tu-proyecto.vercel.app") // Reemplaza con tu URL real de Vercel
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// 3. CONFIGURACIÓN DE AUTENTICACIÓN JWT
var key = Encoding.ASCII.GetBytes("ESTA_ES_UNA_LLAVE_SUPER_SECRETA_Y_LARGA_12345");
builder.Services.AddAuthentication(x =>
{
    x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(x =>
{
    x.RequireHttpsMetadata = false;
    x.SaveToken = true;
    x.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = false,
        ValidateAudience = false
    };
});

var app = builder.Build();

// 4. CONFIGURACIÓN DEL PIPELINE DE HTTP (El orden es muy importante)

// Habilitar Swagger siempre para pruebas iniciales
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "RiftRoulette API v1"));

// Aplicar la política de CORS antes de Auth
app.UseCors("AllowVercel");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
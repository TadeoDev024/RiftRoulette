using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Servicios básicos
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 2. Configuración de CORS para Vercel
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowVercel",
        policy =>
        {
            policy.WithOrigins("https://tu-proyecto.vercel.app") // Cambia por tu URL real de Vercel
                  .AllowAnyHeader()
                  .AllowAnyMethod();
        });
});

// 3. Autenticación JWT
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

// 4. Pipeline de ejecución
app.UseCors("AllowVercel");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
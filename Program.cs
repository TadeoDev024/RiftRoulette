using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text; 

var builder = WebApplication.CreateBuilder(args);

// Agrega esto para solucionar los errores de Swagger
builder.Services.AddEndpointsApiExplorer();


app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
// Program.cs
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("TU_CLAVE_SUPER_SECRETA_DE_64_BITS")),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

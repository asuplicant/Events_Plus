using System.Reflection;
using Azure;
using Azure.AI.ContentSafety;
using Microsoft.Azure.CognitiveServices.ContentModerator;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Projeto_Event_Plus.Context;
using Projeto_Event_Plus.Interfaces;
using Projeto_Event_Plus.Repositories;

var builder = WebApplication.CreateBuilder(args);

// Configura��o do Azure Content Safety.
//var endpoint = builder.Configuration["https://moderatorservicelaura.cognitiveservices.azure.com/"];
//var apiKey = builder.Configuration["DWq7MWkbf34hJdem9r7RatKwj21oHCLKnjQ8pt4N8SJaAWATEWBrJQQJ99BDACYeBjFXJ3w3AAAHACOGKcUY"];

//if(string.IsNullOrEmpty(endpoint) || string.IsNullOrEmpty(apiKey))
//    {
//    throw new InvalidOperationException("Azure Content Safety: Endpoint ou API Key n�o foram configurados.");
//    }

//var client = new ContentSafetyClient(new Uri(endpoint), new AzureKeyCredential(apiKey));
//builder.Services.AddSingleton(client);

builder.Services // Acessa a cole��o de servi�os da aplica��o (Dependency Injection)
    .AddControllers() // Adiciona suporte a controladores na API (MVC ou Web API)
    .AddJsonOptions(options => // Configura as op��es do serializador JSON padr�o (System.Text.Json)
    {
        // Configura��o para ignorar propriedades nulas ao serializar objetos em JSON
        options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;

        // Configura��o para evitar refer�ncia circular ao serializar objetos que possuem relacionamentos recursivos
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

// Adiciona o contexto do banco de dados (exemplo com SQL Server)
builder.Services.AddDbContext<EventPlus_Context>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Adicionar o repositorio e a interface ao container da inje��o de dependencia
builder.Services.AddScoped<ITipoEventoRepository, TipoEventoRepository>();
builder.Services.AddScoped<ITipoUsuarioRepository, TipoUsuarioRepository>();
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<IEventoRepository, EventoRepository>();
builder.Services.AddScoped<IComentarioRepository, ComentarioRepository>();
builder.Services.AddScoped<IPresencaEventosRepository, PresencaEventosRepository>();


// Adiciona o servi�o de Controllers
builder.Services.AddControllers();

builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = "JwtBearer";
    options.DefaultAuthenticateScheme = "JwtBearer";
})

.AddJwtBearer("JwtBearer", options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        // Valida quem est� solicitando.
        ValidateIssuer = true,

        // Valida quem est� recebendo.
        ValidateAudience = true,

        // Define se o tempo de expira��o ser� validado.
        ValidateLifetime = true,

        // Forma de criptografia que valida a chave de autentica��o.
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes("Projeto_Event_Plus-chave-autenticacao-Projeto_Event_Plus")),

        // Valida o tempo de expira��o do Token.
        ClockSkew = TimeSpan.FromMinutes(5),

        // Valida de onde est� vindo.
        ValidIssuer = "Projeto_Event_Plus",

        ValidAudience = "Projeto_Event_Plus"
    };
});

// SWAGGER
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Version = "v1",
        Title = "API Do Event Plus",
        Description = "Aplica��o para gerenciamento de eventos",
        TermsOfService = new Uri("https://example.com/terms"),
        Contact = new OpenApiContact
        {
            Name = "Laura",
            Url = new Uri("https://example.com/contact")
        },
        License = new OpenApiLicense
        {
            Name = "Example License",
            Url = new Uri("https://example.com/license")
        }
    });

    // using System.Refletion
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, xmlFilename));



    // Usando a autentica��o no Swagger
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme()
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Value: Bearer TokenJWT ",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
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
            new string[] {}
        }
    });


});

//CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy",
        builder =>
        {
            builder.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod();
        });
});

builder.Services.AddSingleton(provider => new ContentModeratorClient(
    new ApiKeyServiceClientCredentials("DWq7MWkbf34hJdem9r7RatKwj21oHCLKnjQ8pt4N8SJaAWATEWBrJQQJ99BDACYeBjFXJ3w3AAAHACOGKcUY"))
{
    Endpoint = "https://moderatorservicelaura.cognitiveservices.azure.com/"

});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger(options =>
    {
        options.SerializeAsV2 = true;
    });

    app.UseSwaggerUI(options => // UseSwaggerUI is called only in Development.
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "v1");
        options.RoutePrefix = string.Empty;
    });
}

// Aplicar o servi�o cognitivo

// Habilita o servi�o de moderador de conte�do de Microsoft Azure

// Adiciona o Cors(pol�tica criada)
app.UseCors("CorsPolicy");

// Adicionar o mapeamento dos controllers
app.MapControllers();

app.UseAuthentication();

app.UseAuthorization();

app.Run();


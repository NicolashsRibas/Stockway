using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using StrockWay.Data;
using StrockWay.Middleware;
using StrockWay.Services;

// StrockWay - API REST de gerenciamento de estoque (almoxarifado)
// Executar:  dotnet run              (servidor em http://localhost:8080)
//            dotnet run -- --exemplo (cadastra produtos de exemplo se o banco estiver vazio)

var carregarExemplos = args.Contains("--exemplo");
var builder = WebApplication.CreateBuilder(args.Where(a => a != "--exemplo").ToArray());

// ---- Model: banco SQLite via Entity Framework ----
builder.Services.AddDbContext<StrockWayContext>(opcoes =>
    opcoes.UseSqlite(builder.Configuration.GetConnectionString("StrockWay") ?? "Data Source=strockway.db"));

// ---- Services: regras de negócio ----
builder.Services.AddScoped<EstoqueService>();
builder.Services.AddScoped<DashboardService>();

// ---- Controllers + JSON em snake_case ("valor_unitario") e enums em texto ("ACIMA_MAXIMO") ----
builder.Services
    .AddControllers(opcoes => opcoes.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true)
    .AddJsonOptions(opcoes =>
    {
        opcoes.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
        opcoes.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.SnakeCaseLower;
        opcoes.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseUpper));
        // Mantém acentos e aspas legíveis no JSON (sem códigos de escape)
        opcoes.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping;
    })
    .ConfigureApiBehaviorOptions(opcoes => opcoes.InvalidModelStateResponseFactory = ErrosDeValidacao.CriarResposta);

// ---- CORS: libera o front-end (site) para chamar a API ----
builder.Services.AddCors(opcoes =>
    opcoes.AddDefaultPolicy(politica => politica.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// ---- Swagger: documentação e testes em http://localhost:8080/swagger ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opcoes =>
{
    opcoes.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "StrockWay API",
        Version = "v1",
        Description = "API de gerenciamento de estoque (almoxarifado): produtos, movimentações, alertas e dashboard."
    });

    var comentarios = Path.Combine(AppContext.BaseDirectory, "StrockWay.xml");
    if (File.Exists(comentarios)) opcoes.IncludeXmlComments(comentarios);
});

var app = builder.Build();

// Cria o banco na primeira execução e, se pedido, carrega dados de exemplo.
using (var escopo = app.Services.CreateScope())
{
    var db = escopo.ServiceProvider.GetRequiredService<StrockWayContext>();
    db.Database.EnsureCreated();

    if (carregarExemplos)
    {
        var servico = escopo.ServiceProvider.GetRequiredService<EstoqueService>();
        await DadosExemplo.CarregarAsync(db, servico, app.Logger);
    }
}

app.UseCors();
app.UseMiddleware<TratamentoErrosMiddleware>();

app.UseSwagger();
app.UseSwaggerUI(opcoes => opcoes.DocumentTitle = "StrockWay API");

app.MapControllers();
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

app.Run();

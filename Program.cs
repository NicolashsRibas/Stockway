using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using StockWay.Controllers;
using StockWay.Data;
using StockWay.Middleware;
using StockWay.Services;

// StockWay - API REST de gerenciamento de estoque (almoxarifado)
// Executar:  dotnet run              (servidor em http://localhost:8080)
//            dotnet run -- --exemplo (cadastra produtos de exemplo se o banco estiver vazio)

var carregarExemplos = args.Contains("--exemplo");
var builder = WebApplication.CreateBuilder(args.Where(a => a != "--exemplo").ToArray());

// ---- Model: banco MySQL via Entity Framework ----
var connectionString = builder.Configuration.GetConnectionString("StockWay")
    ?? throw new InvalidOperationException(
        "A string de conexão não foi configurada. Defina ConnectionStrings__StockWay.");

builder.Services.AddDbContext<StockWayContext>(opcoes =>
    opcoes.UseMySql(
        connectionString,
        new MySqlServerVersion(new Version(8, 0, 0)),
        mysql => mysql.EnableRetryOnFailure())
    .UseSnakeCaseNamingConvention());

// ---- Services: regras de negócio ----
builder.Services.AddScoped<EstoqueService>();
builder.Services.AddScoped<DashboardService>();
builder.Services.AddScoped<AutenticacaoService>();
builder.Services.AddSingleton<GeradorDeToken>();

// ---- Relatório diário por e-mail aos gerentes, pela API externa do Brevo ----
var brevo = ConfiguracaoBrevo.Carregar(builder.Configuration);
var configRelatorios = ConfiguracaoRelatorios.Carregar(builder.Configuration);
builder.Services.AddSingleton(brevo);
builder.Services.AddSingleton(configRelatorios);
builder.Services.AddHttpClient<IEnvioDeEmail, BrevoEmailService>(cliente =>
{
    cliente.BaseAddress = new Uri(brevo.Url);
    cliente.Timeout = TimeSpan.FromSeconds(30);
});
builder.Services.AddScoped<RelatorioDiarioService>();
builder.Services.AddScoped<EnvioDeRelatoriosService>();
builder.Services.AddHostedService<AgendadorDeRelatorios>();

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

// ---- Login: token JWT (POST /api/auth/login) ----
var autenticacao = ConfiguracaoAutenticacao.Carregar(builder.Configuration);
builder.Services.AddSingleton(autenticacao);

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opcoes =>
    {
        opcoes.MapInboundClaims = false;   // mantém os nomes curtos do token: sub, name, email, cargo
        opcoes.TokenValidationParameters = new TokenValidationParameters
        {
            ValidIssuer = autenticacao.Emissor,
            ValidAudience = autenticacao.Emissor,
            IssuerSigningKey = autenticacao.ChaveDeAssinatura,
            ValidateIssuerSigningKey = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = UsuarioLogado.ClaimNome,
            RoleClaimType = UsuarioLogado.ClaimCargo
        };
        // Respostas 401/403 no mesmo formato JSON do resto da API.
        opcoes.Events = new JwtBearerEvents
        {
            OnChallenge = async contexto =>
            {
                contexto.HandleResponse();
                contexto.Response.Headers.WWWAuthenticate = "Bearer";
                var mensagem = contexto.AuthenticateFailure is SecurityTokenExpiredException
                    ? "sua sessão expirou; faça login novamente"
                    : "faça login para acessar este recurso";
                await TratamentoErrosMiddleware.EscreverErroAsync(contexto.HttpContext, StatusCodes.Status401Unauthorized, mensagem);
            },
            OnForbidden = contexto => TratamentoErrosMiddleware.EscreverErroAsync(
                contexto.HttpContext, StatusCodes.Status403Forbidden, "você não tem permissão para acessar este recurso")
        };
    });

builder.Services.AddAuthorization(opcoes =>
{
    // "ExigirLogin": true => todas as rotas pedem token, menos as marcadas com [AllowAnonymous]
    // (login, cadastro e health). Com false, as rotas antigas continuam abertas, como antes.
    if (autenticacao.ExigirLogin)
        opcoes.FallbackPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();

    // Rotas de relatório: só quem está logado e tem cargo de gerente (o cargo vem no token).
    opcoes.AddPolicy(RelatoriosController.PoliticaGerente, politica => politica
        .RequireAuthenticatedUser()
        .RequireAssertion(contexto => configRelatorios.EhDestinatario(contexto.User.FindFirst(UsuarioLogado.ClaimCargo)?.Value)));
});

// ---- Limite de tentativas no login e no cadastro: 10 por minuto por IP ----
builder.Services.AddRateLimiter(opcoes =>
{
    opcoes.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    opcoes.AddPolicy(AutenticacaoController.PoliticaDeLimite, contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
            _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
    opcoes.OnRejected = (contexto, _) => new ValueTask(TratamentoErrosMiddleware.EscreverErroAsync(
        contexto.HttpContext, StatusCodes.Status429TooManyRequests,
        "muitas tentativas seguidas; aguarde um minuto e tente novamente"));
});

// ---- CORS: quais sites podem chamar a API ----
// Liberados: os endereços de "Cors:OrigensPermitidas" e, com "Cors:PermitirLocalhost": true,
// qualquer porta de localhost/127.0.0.1 (o front em Vite roda em http://localhost:5173).
var origensPermitidas = builder.Configuration.GetSection("Cors:OrigensPermitidas").Get<string[]>() ?? [];
var permitirLocalhost = builder.Configuration.GetValue("Cors:PermitirLocalhost", true);
builder.Services.AddCors(opcoes =>
    opcoes.AddDefaultPolicy(politica => politica
        .SetIsOriginAllowed(origem =>
            origensPermitidas.Contains(origem, StringComparer.OrdinalIgnoreCase) ||
            (permitirLocalhost && Uri.TryCreate(origem, UriKind.Absolute, out var uri) && uri.IsLoopback))
        .AllowAnyHeader()
        .AllowAnyMethod()));

// ---- Swagger: documentação e testes em http://localhost:8080/swagger (só em desenvolvimento) ----
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(opcoes =>
{
    opcoes.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "StockWay API",
        Version = "v1",
        Description = "API de gerenciamento de estoque (almoxarifado): produtos, movimentações, alertas, dashboard e login."
    });

    // Botão "Authorize" no Swagger: cole o token recebido em POST /api/auth/login.
    opcoes.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Cole aqui o token recebido em POST /api/auth/login (sem a palavra Bearer)."
    });
    opcoes.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        }] = []
    });

    var comentarios = Path.Combine(AppContext.BaseDirectory, "StockWay.xml");
    if (File.Exists(comentarios)) opcoes.IncludeXmlComments(comentarios);
});

var app = builder.Build();

if (autenticacao.ChaveTemporaria)
    app.Logger.LogWarning(
        "Autenticacao:Chave não configurada: usando uma chave temporária. Os logins deixam de valer quando a API " +
        "reinicia. Defina a variável de ambiente Autenticacao__Chave (mínimo de 32 caracteres).");

// Cria o banco e as tabelas na primeira execução e, se pedido, carrega dados de exemplo.
using (var escopo = app.Services.CreateScope())
{
    var db = escopo.ServiceProvider.GetRequiredService<StockWayContext>();
    PreparacaoDoBanco.Executar(db);

    if (carregarExemplos)
    {
        var servico = escopo.ServiceProvider.GetRequiredService<EstoqueService>();
        await DadosExemplo.CarregarAsync(db, servico, app.Logger);
    }
}

app.UseCors();
app.UseMiddleware<TratamentoErrosMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(opcoes => opcoes.DocumentTitle = "StockWay API");
}

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllers();
app.MapGet("/", () => Results.Redirect(app.Environment.IsDevelopment() ? "/swagger" : "/api/health"))
    .AllowAnonymous()
    .ExcludeFromDescription();

app.Run();

# StockWay — Back-end em C# (ASP.NET Core Web API)

**StockWay** é o back-end de um site de gerenciamento de estoque (almoxarifado), feito em **C# com ASP.NET Core 8**, **Entity Framework Core** e **MySQL**, organizado no padrão **MVC**.

**Funcionalidades**

- Cadastro, consulta, edição e remoção de produtos
- Entradas, saídas e ajustes de inventário, com atualização automática da quantidade em estoque
- Log (histórico) de todas as movimentações, pronto para exibir no site
- Dashboard com indicadores, gráficos e alertas
- Identificação de produtos com **baixo estoque**, **acima do máximo** e **vencidos/vencendo**
- Verificações em todos os campos, com mensagens de erro em português
- **Login** com nome, cargo, e-mail e senha (senha guardada só como hash PBKDF2, acesso por token JWT)
- **Relatório diário por e-mail** aos gerentes, pela API do Brevo: entradas e saídas do dia, produtos vencendo em até 7 dias e produtos no estoque mínimo
- Documentação interativa (**Swagger**, em desenvolvimento) e CORS liberado para o front-end em `localhost`

---

## 1. Atributos do produto (`Models/Produto.cs`)

| Campo JSON         | Tipo C#     | Descrição / verificação                                                        |
|--------------------|-------------|--------------------------------------------------------------------------------|
| `codigo`           | `string`    | Obrigatório, único, até 30 caracteres (letras, números, `-` `_` `.`). Salvo em maiúsculas |
| `nome`             | `string`    | Obrigatório, de 2 a 100 caracteres                                             |
| `quantidade`       | `int`       | ≥ 0. No cadastro, não pode passar do máximo. Depois, só muda por movimentação  |
| `valor_unitario`   | `decimal`   | Obrigatório, ≥ 0 (R$)                                                          |
| `valor_em_estoque` | `decimal`   | **Calculado:** `quantidade × valor_unitario`                                   |
| `peso_kg`          | `decimal`   | Obrigatório, > 0, peso de **uma** unidade (máx. 50.000 kg)                     |
| `peso_total_kg`    | `decimal`   | **Calculado:** `quantidade × peso_kg`                                          |
| `validade`         | `DateOnly?` | Opcional, `AAAA-MM-DD`. Sem validade = não perecível. Não aceita data vencida ao cadastrar ou alterar |
| `endereco`         | objeto      | `corredor` e `prateleira`: obrigatórios, até 10 caracteres (letras, números, `-`) |
| `estoque_minimo`   | `int`       | Indicador de mínimo, ≥ 0 e menor que o máximo                                  |
| `estoque_maximo`   | `int`       | Indicador de máximo, > 0                                                       |

**Campos calculados na resposta:** `status` (`ZERADO`, `BAIXO`, `NORMAL`, `ACIMA_MAXIMO`), `baixo_estoque`, `quantidade_para_repor`, `percentual_ocupacao`, `status_validade` (`NAO_PERECIVEL`, `OK`, `VENCE_EM_BREVE` (até 30 dias), `VENCIDO`) e `dias_para_vencer`.

### Onde ficam as verificações

As verificações ficam **dentro dos Models**, de modo que um `Produto` nunca existe em estado inválido:

- **Regras de um único atributo ficam no setter.** Exemplos: limite de caracteres do nome, peso entre 0 e 50.000 kg, formato do código e do endereço, validade não vencida. Qualquer atribuição como `produto.PesoKg = -1` lança `RegraNegocioException`, e a API devolve 400 com a mensagem.
- **Regras que envolvem mais de um atributo ficam em métodos.**
  - `DefinirLimites(minimo, maximo)`: o mínimo precisa ser menor que o máximo. Com dois setters separados, a ordem das atribuições poderia gerar um erro falso.
  - `RegistrarEntrada`, `RegistrarSaida`, `AjustarPara` e `Desativar`.
  - Por isso `Quantidade`, `EstoqueMinimo`, `EstoqueMaximo`, `Codigo` e `Ativo` têm **setter privado**: só mudam por esses métodos.
- **O construtor** `new Produto(...)` exige todos os dados obrigatórios e passa por todos os setters.
- **Ao ler do banco**, o Entity Framework usa o construtor privado e grava direto nos campos privados (`_nome`, `_pesoKg`...). Assim, as verificações não rodam de novo e não bloqueiam a leitura de dados antigos.
- **`Movimentacao` é imutável**, porque é um registro de auditoria. Ela só é criada pelo construtor, e todos os setters são privados.
- **`Usuario` segue a mesma ideia:** nome, cargo e e-mail são validados nos setters, e a senha **nunca** é guardada. O construtor e `AlterarSenha` recebem a senha digitada, validam e guardam só o hash (`SenhaHash`). Para conferir o login, use `ConferirSenha`.

---

## 2. Estrutura MVC

```
StockWay/
├── Program.cs                    # configuração: banco, JSON, CORS, Swagger, rotas
├── StockWay.csproj
├── appsettings.json              # porta (8080) e conexão padrão com o banco
├── Models/                       # MODEL: classes com atributos, campos calculados e verificações
│   ├── Produto.cs
│   ├── Movimentacao.cs           #   registro do log
│   ├── Usuario.cs                #   quem entra no sistema: nome, cargo, e-mail e hash da senha
│   └── Enums.cs                  #   StatusEstoque, StatusValidade, TipoMovimentacao
├── Data/
│   ├── StockWayContext.cs       # Entity Framework (banco MySQL stockway)
│   └── PreparacaoDoBanco.cs     # cria o banco e as tabelas ao iniciar (inclusive as novas)
├── Services/                     # regras de negócio
│   ├── EstoqueService.cs         #   cadastro, entradas, saídas, ajustes, log
│   ├── DashboardService.cs       #   indicadores da dashboard
│   ├── AutenticacaoService.cs    #   cadastro de usuário e login
│   ├── GeradorDeToken.cs         #   gera o token JWT entregue no login
│   ├── IEnvioDeEmail.cs          #   envio de e-mail por serviço externo
│   ├── BrevoEmailService.cs      #   implementação com a API do Brevo
│   ├── RelatorioDiarioService.cs #   dados do relatório dos gerentes
│   ├── RelatorioDiarioEmail.cs   #   transforma o relatório em e-mail (HTML e texto)
│   ├── EnvioDeRelatoriosService.cs # envia aos gerentes e grava o histórico
│   ├── AgendadorDeRelatorios.cs  #   envio automático no fim do dia
│   └── DadosExemplo.cs
├── DTOs/                         # VIEW: formato do JSON de entrada (Requests) e de saída (Responses)
│   ├── Requests.cs
│   ├── Responses.cs
│   └── RespostaApi.cs            #   envelope { sucesso, dados | erro }
├── Controllers/                  # CONTROLLER: recebem as requisições HTTP e chamam os Services
│   ├── ProdutosController.cs
│   ├── MovimentacoesController.cs
│   ├── EstoqueController.cs      #   baixo estoque, alertas, validade
│   ├── DashboardController.cs
│   ├── AutenticacaoController.cs #   /api/auth: cadastro, login e usuário logado
│   └── RelatoriosController.cs   #   /api/relatorios: prévia, envio e histórico (gerentes)
├── Common/
│   ├── Relogio.cs                # data/hora do sistema (usado por Models e Services)
│   ├── HashDeSenha.cs            # hash e conferência de senha (PBKDF2-SHA512)
│   ├── ConfiguracaoAutenticacao.cs # seção "Autenticacao" do appsettings.json
│   └── UsuarioLogado.cs          # lê os dados do token (id, nome) nos Controllers
├── Middleware/                   # tratamento de erros e mensagens de validação em português
├── Exceptions/
│   └── RegraNegocioException.cs
└── docs/
    ├── requisicoes.http          # todas as rotas para testar no VS Code
    └── frontend/stockway-api.js # cliente JavaScript pronto para o site
```

**Fluxo de uma requisição:**

```
Site ──HTTP──▶ Controller ──▶ Service (regras) ──▶ Model + banco (EF Core / MySQL)
                   │
Site ◀──JSON── DTO (View) ◀─┘
```

Numa API, a "View" do MVC não é uma página HTML, e sim o JSON devolvido. Esse formato fica definido nos DTOs de resposta.

---

## 3. Como executar

1. Instale o **.NET SDK 8** (ou mais recente):
   ```bash
   winget install Microsoft.DotNet.SDK.8 --source winget
   ```
   Depois feche e abra o VS Code de novo.
2. No VS Code, instale a extensão **C# Dev Kit**. O próprio VS Code sugere as extensões recomendadas.
3. Instale o **MySQL Server 8** (ou MariaDB compatível) e deixe o serviço rodando na porta `3306`.
4. Configure a conexão. A configuração padrão (`appsettings.json`) espera MySQL local com usuário `root` e senha `admin`:
   ```text
   Server=localhost;Port=3306;Database=stockway;User=root;Password=admin;
   ```
   Se sua instalação tiver outra senha ou outro usuário, defina a variável de ambiente
   `ConnectionStrings__StockWay` antes de iniciar. No PowerShell:
   ```powershell
   $env:ConnectionStrings__StockWay="Server=localhost;Port=3306;Database=stockway;User=stockway_app;Password=sua_senha;"
   ```
   O arquivo [`Scripts/Sql/create_schema_tables.sql`](Scripts/Sql/create_schema_tables.sql) também pode ser executado no MySQL Workbench. A aplicação cria o banco e as tabelas automaticamente ao iniciar, portanto executar o script manualmente é opcional. Em um banco que já existia, a tabela nova `usuarios` também é criada sozinha.
5. No terminal, dentro da pasta do projeto:
   ```bash
   dotnet run -- --exemplo
   ```
   Na primeira vez, o .NET baixa os pacotes automaticamente. A opção `--exemplo` cadastra 7 produtos de exemplo se o banco estiver vazio.
6. Abra **http://localhost:8080/swagger** no navegador. O Swagger lista todas as rotas e permite testar cada uma clicando em **Try it out**. Para testar rotas com login, faça `POST /api/auth/login`, copie o `token` e cole no botão **Authorize**.

   O Swagger só aparece em desenvolvimento (`ASPNETCORE_ENVIRONMENT=Development`, que é o padrão do `dotnet run`).

Para começar do zero, pare a API e remova o banco `stockway` no MySQL ou execute o script em um banco vazio.

---

## 4. APIs

URL base: `http://localhost:8080/api`. Os campos JSON usam **snake_case** (`valor_unitario`, `peso_kg`...).

Todas as respostas seguem o mesmo formato:

```json
{ "sucesso": true,  "dados": { ... } }
{ "sucesso": false, "erro": "estoque insuficiente para 'PAR-001': solicitado 10, disponível 4" }
```

| Código HTTP | Quando                                                        |
|-------------|---------------------------------------------------------------|
| 200 / 201   | Sucesso (201 = registro criado)                               |
| 400         | Dados inválidos (a mensagem indica o campo)                   |
| 401         | Sem login, e-mail/senha inválidos ou sessão expirada          |
| 403         | Usuário desativado, ou sem permissão (ex.: relatórios são só para gerentes) |
| 404         | Produto não encontrado                                        |
| 409         | Código ou e-mail duplicado, remoção de produto que ainda tem saldo, ou produto alterado por outra operação ao mesmo tempo |
| 422         | Saída maior que o saldo, ou entrada que ultrapassa o máximo   |
| 502         | O serviço de e-mail (Brevo) recusou ou não respondeu          |
| 429         | Muitas tentativas de login/cadastro seguidas (limite: 10 por minuto) |
| 500         | Erro interno                                                  |

**Campo `responsavel`:** sem login, ele é obrigatório no corpo das operações (como sempre foi). Com login (token no cabeçalho `Authorization`), o `responsavel` enviado é **ignorado** e o nome do usuário logado é usado. Assim ninguém registra uma operação em nome de outra pessoa.

### 4.1 Produtos

| Método | Rota                                   | Descrição                                       |
|--------|----------------------------------------|-------------------------------------------------|
| GET    | `/produtos`                            | Lista e pesquisa produtos                       |
| POST   | `/produtos`                            | Cadastra um produto                             |
| GET    | `/produtos/{codigo}`                   | Consulta um produto e suas 10 últimas movimentações |
| PUT    | `/produtos/{codigo}`                   | Altera dados cadastrais (envie só o que mudou)  |
| DELETE | `/produtos/{codigo}`                   | Remove o produto (somente com saldo zero)       |
| GET    | `/produtos/{codigo}/movimentacoes`     | Histórico do produto                            |

**Filtros de `GET /produtos`** (todos opcionais):

- `busca`: parte do nome ou do código
- `corredor`
- `status`: `ZERADO`, `BAIXO`, `NORMAL` ou `ACIMA_MAXIMO`
- `validade`: `NAO_PERECIVEL`, `OK`, `VENCE_EM_BREVE` ou `VENCIDO`
- `ordenar`: `nome`, `codigo`, `quantidade`, `valor_em_estoque`, `endereco`, `validade` ou `urgencia`
- `incluir_inativos=true`

**Cadastro (`POST /produtos`):**

```json
{
  "codigo": "LUV-020",
  "nome": "Luva de raspa (par)",
  "quantidade": 40,
  "valor_unitario": 12.50,
  "peso_kg": 0.18,
  "validade": "2027-12-31",
  "endereco": { "corredor": "B", "prateleira": "04" },
  "estoque_minimo": 15,
  "estoque_maximo": 150,
  "responsavel": "Maria Souza"
}
```

**Edição (`PUT /produtos/{codigo}`):** envie apenas os campos que mudaram, junto com `responsavel`. O campo `quantidade` é recusado aqui, porque o saldo só muda por movimentação e assim tudo fica no log. Para tirar a validade, envie `"remover_validade": true`. Cada edição grava no histórico o que mudou.

**Remoção (`DELETE /produtos/{codigo}`):** corpo `{"responsavel": "...", "motivo": "..."}`. A remoção é lógica, então o histórico é preservado.

### 4.2 Movimentações (atualizam a quantidade em estoque)

| Método | Rota                         | Corpo JSON                                                                            |
|--------|------------------------------|---------------------------------------------------------------------------------------|
| POST   | `/movimentacoes/entrada`     | `codigo`, `quantidade`, `responsavel`, `observacao`?, `valor_unitario`?               |
| POST   | `/movimentacoes/saida`       | `codigo`, `quantidade`, `responsavel`, `observacao`?                                  |
| POST   | `/movimentacoes/ajuste`      | `codigo`, `quantidade_contada`, `responsavel`, `observacao` (**obrigatória**: motivo) |
| GET    | `/movimentacoes`             | Log. Filtros: `codigo`, `tipo`, `data_inicio`, `data_fim` (AAAA-MM-DD), `limite` (1–1000, padrão 50), `offset` |

Regras:

- **Entrada:** é recusada se ultrapassar o `estoque_maximo`. Se você informar `valor_unitario`, o valor do produto passa a ser o **custo médio ponderado**.
- **Saída:** é recusada se a quantidade for maior que o saldo.
- **Ajuste:** define o saldo como a quantidade contada no inventário. O log guarda a diferença, que fica negativa quando há perda.

A resposta traz a `movimentacao`, o `produto` atualizado e uma lista de `alertas`, por exemplo: `"produto atingiu o estoque mínimo; reposição sugerida de 71 unidade(s)"`.

Tipos de registro no log: `CADASTRO`, `ENTRADA`, `SAIDA`, `AJUSTE`, `EDICAO`, `REMOCAO`.

### 4.3 Alertas e dashboard

| Método | Rota                         | Descrição                                                                 |
|--------|------------------------------|---------------------------------------------------------------------------|
| GET    | `/estoque/baixo`             | Zerados ou abaixo do mínimo (mais urgentes primeiro) e custo estimado de reposição |
| GET    | `/estoque/alertas`           | `baixo_estoque`, `acima_maximo` e `validade`                              |
| GET    | `/estoque/validade?dias=30`  | Vencidos e que vencem nos próximos N dias                                 |
| GET    | `/dashboard`                 | Tudo o que a tela inicial precisa                                         |
| GET    | `/health`                    | Verifica se a API está no ar                                              |

`GET /dashboard` retorna:

- `resumo`: total de produtos, unidades, **valor total em estoque**, **peso total**, baixo estoque, vencidos e vencendo
- `estoque_por_status`: para gráfico de pizza
- `movimentacoes_hoje`: entradas e saídas do dia
- `movimentacoes_semana`: entradas × saídas por dia nos últimos 7 dias, para gráfico de barras
- `alertas_baixo_estoque`, `alertas_acima_maximo`, `alertas_validade` e `maiores_valores_em_estoque`
- `estoque_por_corredor`
- `ultimas_movimentacoes`: o log recente

### 4.4 Login (`/api/auth`)

| Método | Rota                 | Login?  | Descrição                                          |
|--------|----------------------|---------|----------------------------------------------------|
| POST   | `/auth/cadastro`     | não*    | Cria um usuário: `nome`, `cargo`, `email`, `senha` |
| POST   | `/auth/login`        | não     | Confere `email` e `senha` e devolve o token        |
| GET    | `/auth/eu`           | **sim** | Dados de quem está logado                          |

\* Com `"CadastroAberto": false`, só um usuário logado cria contas novas (o primeiro usuário pode sempre ser criado).

**Atributos do usuário (`Models/Usuario.cs`):**

| Campo JSON        | Verificação                                                                   |
|-------------------|-------------------------------------------------------------------------------|
| `nome`            | Obrigatório, de 2 a 60 caracteres (é o nome que aparece como responsável no log) |
| `cargo`           | Obrigatório, de 2 a 60 caracteres (ex.: "Almoxarife")                         |
| `email`           | Obrigatório, válido, único, até 255 caracteres. Salvo em minúsculas           |
| `senha`           | Obrigatória, de 8 a 128 caracteres, com pelo menos uma letra e um número. Só o hash é guardado (`senha_hash`) |

**Cadastro (`POST /auth/cadastro`):**

```json
{ "nome": "Maria Souza", "cargo": "Almoxarife", "email": "maria@empresa.com", "senha": "trocar123" }
```

**Login (`POST /auth/login`):** corpo `{ "email": "...", "senha": "..." }`. Resposta:

```json
{
  "sucesso": true,
  "dados": {
    "token": "eyJhbGciOi...",
    "tipo_token": "Bearer",
    "expira_em": "2026-11-08T06:00:00Z",
    "usuario": { "id": 1, "nome": "Maria Souza", "cargo": "Almoxarife", "email": "maria@empresa.com", "ativo": true, "criado_em": "...", "ultimo_acesso_em": "..." }
  }
}
```

Nas próximas requisições, envie o cabeçalho `Authorization: Bearer <token>`. E-mail inexistente e senha errada dão a mesma mensagem (`e-mail ou senha inválidos`), para não revelar quais e-mails estão cadastrados.

**Configuração (`appsettings.json`, seção `Autenticacao`):**

| Chave               | Padrão     | O que faz                                                                  |
|---------------------|------------|----------------------------------------------------------------------------|
| `ExigirLogin`       | `true`     | Todas as rotas pedem token, menos login, cadastro e `/health`. `false` deixa as rotas de estoque abertas |
| `CadastroAberto`    | `true`     | `false` = só usuário logado cadastra outros                                |
| `ExpiracaoMinutos`  | `480`      | Validade do token (8 horas)                                                |
| `Emissor`           | `StockWay` | Nome gravado no token                                                      |
| `Chave`             | (vazio)    | Segredo que assina os tokens, com pelo menos 32 caracteres. **Não coloque no appsettings.json**: use User Secrets ou a variável de ambiente `Autenticacao__Chave` |

Sem `Chave`, a API gera uma chave temporária ao iniciar (e avisa no terminal). Funciona, mas todos precisam entrar de novo sempre que a API reinicia. Para gravar uma chave aleatória no seu computador (desenvolvimento), rode na pasta do projeto, no PowerShell:

```powershell
$b = New-Object byte[] 48; [Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($b)
dotnet user-secrets set "Autenticacao:Chave" ([Convert]::ToBase64String($b))
```

No servidor (produção), use a variável de ambiente `Autenticacao__Chave`.

**Com o login obrigatório (`ExigirLogin: true`), o primeiro passo em um banco novo é criar uma conta** pela tela "Criar conta" do site (ou `POST /api/auth/cadastro`). No Swagger, faça `POST /api/auth/login`, copie o `token` e cole no botão **Authorize** para testar as outras rotas.

**CORS (seção `Cors`):** com `"PermitirLocalhost": true`, o front pode chamar a API de qualquer porta de `localhost`/`127.0.0.1` (o Vite usa `http://localhost:5173`). Para liberar outro endereço, coloque-o em `"OrigensPermitidas"`, por exemplo `["https://stockway.minhaempresa.com"]`.

### 4.5 Relatório diário por e-mail aos gerentes (Brevo)

Todo fim de dia a API manda um e-mail para **todos os usuários ativos cujo cargo contém "gerente"** (vale "Gerente", "gerente de estoque", "GERENTE"...). O e-mail é entregue pela **API externa do [Brevo](https://www.brevo.com)** (`POST https://api.brevo.com/v3/smtp/email`) e traz:

- **Entradas e saídas** do período: quantas foram e quantas unidades, e o total por produto.
- **Produtos que vencem em até 7 dias**: um produto que vence daqui a 7 dias já aparece no relatório de hoje. Produtos já vencidos que ainda têm saldo também aparecem (marcados como vencidos). Produtos sem saldo ficam de fora.
- **Produtos no estoque mínimo ou abaixo**, os mais urgentes primeiro, com quanto repor.

**Período das movimentações:** cada relatório continua de onde o anterior parou. Com o envio às 18:00, o relatório de hoje cobre de ontem às 18:00 até hoje às 18:00, então nada registrado depois do horário fica de fora. O primeiro relatório começa à meia-noite.

**Como ligar (uma vez):**

1. Crie uma conta grátis no Brevo (até 300 e-mails por dia).
2. Em **Senders, Domains & Dedicated IPs > Senders**, cadastre e confirme o e-mail que vai aparecer como remetente (pode ser um Gmail).
3. Em **SMTP & API > API Keys**, gere uma chave de API.
4. Guarde a chave e o remetente. **No seu computador (desenvolvimento)**, use os User Secrets do .NET, na pasta do projeto. Eles ficam gravados no seu usuário do Windows, fora do projeto, então nunca vão para o git:
   ```powershell
   dotnet user-secrets set "Brevo:ChaveApi" "xkeysib-..."
   dotnet user-secrets set "Brevo:RemetenteEmail" "estoque@suaempresa.com.br"
   ```
   **No servidor (produção)**, use variáveis de ambiente (`Brevo__ChaveApi` e `Brevo__RemetenteEmail`); os User Secrets só valem com `ASPNETCORE_ENVIRONMENT=Development`.

   Sem a chave, a API funciona normalmente, mas avisa no terminal que o envio automático está parado.
5. **Se o envio der "unrecognised IP address" (HTTP 401):** o Brevo bloqueia chaves usadas de IPs novos. Em **Security > Authorized IPs**, autorize o IP do computador ou desative o bloqueio.

**Configuração (`appsettings.json`):**

| Chave                            | Padrão     | O que faz                                                         |
|----------------------------------|------------|-------------------------------------------------------------------|
| `Relatorios:EnvioAutomatico`     | `true`     | `false` desliga o envio de fim de dia                             |
| `Relatorios:Horario`             | `18:00`    | Horário do envio (horário do servidor)                            |
| `Relatorios:DiasAvisoValidade`   | `7`        | Produtos que vencem em até N dias entram no relatório             |
| `Relatorios:CargoDestinatario`   | `gerente`  | Recebe quem tem este texto no cargo (sem diferenciar acento ou maiúscula) |
| `Brevo:RemetenteNome`            | `StockWay` | Nome que aparece como remetente                                   |
| `Brevo:ChaveApi` / `Brevo:RemetenteEmail` | (vazio) | Use as variáveis de ambiente acima                         |

**Se a API estiver desligada no horário,** o relatório sai assim que ela ligar, no mesmo dia. **Se o Brevo falhar,** a API tenta de novo a cada 30 minutos, até 5 vezes no dia. Reiniciar a API depois do envio não manda de novo. Com duas cópias da API rodando ao mesmo tempo, as duas podem enviar no mesmo minuto; nesse caso, deixe `Relatorios:EnvioAutomatico` em `true` em só uma delas.

**Rotas (só para usuários logados com cargo de gerente; outros recebem 403):**

| Método | Rota                             | Descrição                                                      |
|--------|----------------------------------|----------------------------------------------------------------|
| GET    | `/relatorios/diario`             | Prévia do relatório que sairá no fim do dia (não envia nada)   |
| POST   | `/relatorios/diario/enviar`      | Envia agora para todos os gerentes (o envio automático continua) |
| GET    | `/relatorios/envios?limite=30`   | Histórico dos envios                                           |

Cada envio (automático ou manual) fica gravado na tabela **`relatorios_enviados`**: período, destinatários, situação (`Enviado`, `Falhou` ou `SemDestinatarios`), o identificador do e-mail no Brevo ou o motivo da falha, e quem pediu (no envio manual).

Para trocar de provedor (SendGrid, Resend...), basta criar outra classe que implemente `Services/IEnvioDeEmail.cs` e registrá-la no `Program.cs` no lugar do `BrevoEmailService`.

---

## 5. Como usar no seu site (front-end)

1. Deixe a API rodando (`dotnet run`).
2. Copie [`docs/frontend/stockway-api.js`](docs/frontend/stockway-api.js) para o projeto do site.
3. Use as funções prontas:

```html
<script type="module">
  import { api } from "./stockway-api.js";

  // Cards da dashboard
  const dash = await api.dashboard();
  document.querySelector("#valor-total").textContent =
    dash.resumo.valor_total_estoque.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
  document.querySelector("#baixo-estoque").textContent = dash.resumo.produtos_baixo_estoque;

  // Tabela do log. Use textContent (e não innerHTML) com dados da API: assim um nome de
  // produto como "<script>..." aparece como texto e nunca é executado no navegador.
  const log = await api.listarMovimentacoes({ limite: 20 });
  document.querySelector("#log").replaceChildren(...log.itens.map(m => {
    const linha = document.createElement("tr");
    for (const valor of [new Date(m.data_hora).toLocaleString("pt-BR"), m.tipo_descricao,
                         m.produto_nome, m.quantidade, m.saldo_atual, m.responsavel]) {
      const celula = document.createElement("td");
      celula.textContent = valor;
      linha.append(celula);
    }
    return linha;
  }));

  // Formulário de saída
  document.querySelector("#form-saida").addEventListener("submit", async (e) => {
    e.preventDefault();
    const f = new FormData(e.target);
    try {
      const r = await api.registrarSaida({
        codigo: f.get("codigo"),
        quantidade: Number(f.get("quantidade")),
        responsavel: f.get("responsavel"),
        observacao: f.get("observacao"),
      });
      r.alertas.forEach(a => alert("Atenção: " + a));
    } catch (erro) {
      alert(erro.message);   // mensagem de validação vinda da API
    }
  });
</script>
```

> Envie números como número em JSON (`Number(...)`) e use **ponto** como separador decimal (`12.5`).

Para testar sem o site, use o **Swagger** (`/swagger`) ou abra [`docs/requisicoes.http`](docs/requisicoes.http) com a extensão **REST Client**.

---

## 6. Subir no GitHub

```bash
git init
git add .
git commit -m "Primeiro commit: back-end do StockWay em C# (MVC)"
git branch -M main
git remote add origin https://github.com/SEU_USUARIO/StockWay.git
git push -u origin main
```

O `.gitignore` já impede o envio de `bin/` e `obj/`. Nunca coloque senhas reais nem a chave do login no `appsettings.json`; prefira as variáveis de ambiente `ConnectionStrings__StockWay` e `Autenticacao__Chave`. A senha `admin` do `appsettings.json` serve só para o MySQL local de desenvolvimento.

---

## 7. Observações técnicas

- As operações que alteram o estoque são executadas uma de cada vez dentro da API. Além disso, o saldo (`quantidade`) e o `atualizado_em` do produto funcionam como **controle de concorrência**: se outra operação (inclusive de outra cópia da API rodando ao mesmo tempo) alterou o produto entre a leitura e a gravação, nada é gravado e a resposta é 409. Isso impede que duas saídas simultâneas usem o mesmo saldo.
- O banco MySQL e as tabelas são criados por `Data/PreparacaoDoBanco.cs`: `EnsureCreated()` na primeira execução e `CREATE TABLE IF NOT EXISTS` para tabelas adicionadas depois (como `usuarios`). Se, no futuro, você alterar colunas de tabelas que já existem e quiser manter os dados, migre para *EF Core Migrations*.
- Datas e horas são gravadas no horário local do servidor e saem no JSON sem fuso (`2026-11-07T14:30:00`), tanto logo após gravar quanto ao ler do banco.
- Antes de publicar na internet: mantenha `ExigirLogin` ligado (já vem `true`), considere `CadastroAberto: false`, defina `Autenticacao__Chave`, use HTTPS e coloque o endereço do site em `Cors:OrigensPermitidas`. O Swagger já fica desligado fora de desenvolvimento.

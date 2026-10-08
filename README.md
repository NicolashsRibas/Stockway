# StrockWay — Back-end em C# (ASP.NET Core Web API)

**StrockWay** é o back-end de um site de gerenciamento de estoque (almoxarifado), feito em **C# com ASP.NET Core 8**, **Entity Framework Core** e **SQLite**, organizado no padrão **MVC**.

**Funcionalidades**

- Cadastro, consulta, edição e remoção de produtos
- Entradas, saídas e ajustes de inventário, com atualização automática da quantidade em estoque
- Log (histórico) de todas as movimentações, pronto para exibir no site
- Dashboard com indicadores, gráficos e alertas
- Identificação de produtos com **baixo estoque**, **acima do máximo** e **vencidos/vencendo**
- Verificações em todos os campos, com mensagens de erro em português
- Documentação interativa (**Swagger**) e CORS liberado para o front-end

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

---

## 2. Estrutura MVC

```
StrockWay/
├── Program.cs                    # configuração: banco, JSON, CORS, Swagger, rotas
├── StrockWay.csproj
├── appsettings.json              # porta (8080) e caminho do banco
├── Models/                       # MODEL: classes com atributos, campos calculados e verificações
│   ├── Produto.cs
│   ├── Movimentacao.cs           #   registro do log
│   └── Enums.cs                  #   StatusEstoque, StatusValidade, TipoMovimentacao
├── Data/
│   └── StrockWayContext.cs       # Entity Framework (banco SQLite strockway.db)
├── Services/                     # regras de negócio
│   ├── EstoqueService.cs         #   cadastro, entradas, saídas, ajustes, log
│   ├── DashboardService.cs       #   indicadores da dashboard
│   └── DadosExemplo.cs
├── DTOs/                         # VIEW: formato do JSON de entrada (Requests) e de saída (Responses)
│   ├── Requests.cs
│   ├── Responses.cs
│   └── RespostaApi.cs            #   envelope { sucesso, dados | erro }
├── Controllers/                  # CONTROLLER: recebem as requisições HTTP e chamam os Services
│   ├── ProdutosController.cs
│   ├── MovimentacoesController.cs
│   ├── EstoqueController.cs      #   baixo estoque, alertas, validade
│   └── DashboardController.cs
├── Common/
│   └── Relogio.cs                # data/hora do sistema (usado por Models e Services)
├── Middleware/                   # tratamento de erros e mensagens de validação em português
├── Exceptions/
│   └── RegraNegocioException.cs
└── docs/
    ├── requisicoes.http          # todas as rotas para testar no VS Code
    └── frontend/strockway-api.js # cliente JavaScript pronto para o site
```

**Fluxo de uma requisição:**

```
Site ──HTTP──▶ Controller ──▶ Service (regras) ──▶ Model + banco (EF Core / SQLite)
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
3. No terminal, dentro da pasta do projeto:
   ```bash
   dotnet run -- --exemplo
   ```
   Na primeira vez, o .NET baixa os pacotes automaticamente. A opção `--exemplo` cadastra 7 produtos de exemplo se o banco estiver vazio.
4. Abra **http://localhost:8080/swagger** no navegador. O Swagger lista todas as rotas e permite testar cada uma clicando em **Try it out**.

O banco é o arquivo `strockway.db`, criado automaticamente na pasta do projeto. Para começar do zero, pare o servidor e apague esse arquivo.

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
| 404         | Produto não encontrado                                        |
| 409         | Código duplicado ou remoção de produto que ainda tem saldo    |
| 422         | Saída maior que o saldo, ou entrada que ultrapassa o máximo   |
| 500         | Erro interno                                                  |

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

---

## 5. Como usar no seu site (front-end)

1. Deixe a API rodando (`dotnet run`).
2. Copie [`docs/frontend/strockway-api.js`](docs/frontend/strockway-api.js) para o projeto do site.
3. Use as funções prontas:

```html
<script type="module">
  import { api } from "./strockway-api.js";

  // Cards da dashboard
  const dash = await api.dashboard();
  document.querySelector("#valor-total").textContent =
    dash.resumo.valor_total_estoque.toLocaleString("pt-BR", { style: "currency", currency: "BRL" });
  document.querySelector("#baixo-estoque").textContent = dash.resumo.produtos_baixo_estoque;

  // Tabela do log
  const log = await api.listarMovimentacoes({ limite: 20 });
  document.querySelector("#log").innerHTML = log.itens.map(m => `
    <tr>
      <td>${new Date(m.data_hora).toLocaleString("pt-BR")}</td><td>${m.tipo_descricao}</td>
      <td>${m.produto_nome}</td><td>${m.quantidade}</td><td>${m.saldo_atual}</td><td>${m.responsavel}</td>
    </tr>`).join("");

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
git commit -m "Primeiro commit: back-end do StrockWay em C# (MVC)"
git branch -M main
git remote add origin https://github.com/SEU_USUARIO/StrockWay.git
git push -u origin main
```

O `.gitignore` já impede o envio de `bin/`, `obj/` e do banco `strockway.db`.

---

## 7. Observações técnicas

- As operações que alteram o estoque são executadas uma de cada vez. Isso impede que duas saídas simultâneas usem o mesmo saldo.
- O banco é criado com `EnsureCreated()`. Se, no futuro, você alterar os Models e quiser manter os dados, migre para *EF Core Migrations*.
- A API não tem autenticação. Antes de publicar na internet, adicione login, por exemplo com ASP.NET Core Identity e JWT.

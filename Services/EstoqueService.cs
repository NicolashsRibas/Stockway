using Microsoft.EntityFrameworkCore;
using StrockWay.Data;
using StrockWay.DTOs;
using StrockWay.Exceptions;
using StrockWay.Models;

namespace StrockWay.Services;

/// <summary>
/// Regras de negócio do almoxarifado: cadastro, entradas, saídas, ajustes e histórico.
/// Toda alteração gera um registro em Movimentacoes (log).
/// </summary>
public class EstoqueService(StrockWayContext db)
{
    // Uma operação de escrita por vez: evita que duas saídas simultâneas usem o mesmo saldo.
    private static readonly SemaphoreSlim Trava = new(1, 1);

    public static DateOnly Hoje => DateOnly.FromDateTime(DateTime.Now);

    /// <summary>Data/hora atual sem frações de segundo.</summary>
    public static DateTime Agora
    {
        get
        {
            var agora = DateTime.Now;
            return new DateTime(agora.Ticks - agora.Ticks % TimeSpan.TicksPerSecond, agora.Kind);
        }
    }

    // ==================================================================
    // Consultas
    // ==================================================================

    public async Task<List<Produto>> ListarProdutosAsync(FiltroProdutos filtro)
    {
        var consulta = db.Produtos.AsNoTracking().AsQueryable();
        if (!filtro.IncluirInativos)
            consulta = consulta.Where(p => p.Ativo);
        if (!string.IsNullOrWhiteSpace(filtro.Corredor))
        {
            var corredor = filtro.Corredor.Trim().ToUpperInvariant();
            consulta = consulta.Where(p => p.Corredor == corredor);
        }

        // Os filtros abaixo usam campos calculados, por isso são aplicados em memória.
        IEnumerable<Produto> lista = await consulta.ToListAsync();
        var hoje = Hoje;

        if (!string.IsNullOrWhiteSpace(filtro.Busca))
        {
            var busca = filtro.Busca.Trim();
            lista = lista.Where(p => p.Nome.Contains(busca, StringComparison.OrdinalIgnoreCase) ||
                                     p.Codigo.Contains(busca, StringComparison.OrdinalIgnoreCase));
        }
        if (filtro.Status is StatusEstoque status)
            lista = lista.Where(p => p.Status == status);
        if (filtro.Validade is StatusValidade validade)
            lista = lista.Where(p => p.ObterStatusValidade(hoje) == validade);

        return Ordenar(lista, filtro.Ordenar).ToList();
    }

    public static IEnumerable<Produto> Ordenar(IEnumerable<Produto> lista, string? ordenar) =>
        (ordenar ?? "nome").Trim().ToLowerInvariant() switch
        {
            "nome" or "" => lista.OrderBy(p => p.Nome, StringComparer.CurrentCultureIgnoreCase),
            "codigo" => lista.OrderBy(p => p.Codigo, StringComparer.Ordinal),
            "quantidade" => lista.OrderBy(p => p.Quantidade),
            "valor_em_estoque" => lista.OrderByDescending(p => p.ValorEmEstoque),
            "endereco" => lista.OrderBy(p => p.Corredor).ThenBy(p => p.Prateleira),
            "validade" => lista.OrderBy(p => p.Validade is null).ThenBy(p => p.Validade),
            "urgencia" => OrdenarPorUrgencia(lista),
            _ => throw RegraNegocioException.Validacao(
                "parâmetro 'ordenar' inválido; use nome, codigo, quantidade, valor_em_estoque, endereco, validade ou urgencia")
        };

    /// <summary>Quem precisa de reposição primeiro; dentro disso, menor saldo em relação ao mínimo.</summary>
    public static IEnumerable<Produto> OrdenarPorUrgencia(IEnumerable<Produto> lista) =>
        lista.OrderByDescending(p => p.PrecisaReposicao)
             .ThenBy(p => p.EstoqueMinimo > 0 ? (double)p.Quantidade / p.EstoqueMinimo : p.Quantidade);

    public async Task<Produto> ObterProdutoAsync(string codigo, bool incluirInativos = false)
    {
        var cod = Produto.NormalizarCodigo(codigo);
        var produto = await db.Produtos.FirstOrDefaultAsync(p => p.Codigo == cod && (incluirInativos || p.Ativo));
        return produto ?? throw RegraNegocioException.NaoEncontrado($"produto '{cod}' não encontrado");
    }

    public async Task<List<Movimentacao>> UltimasMovimentacoesAsync(string codigo, int quantidade) =>
        await db.Movimentacoes.AsNoTracking()
            .Where(m => m.ProdutoCodigo == codigo)
            .OrderByDescending(m => m.Id)
            .Take(quantidade)
            .ToListAsync();

    public async Task<(int Total, List<Movimentacao> Itens)> ListarMovimentacoesAsync(FiltroMovimentacoes filtro)
    {
        var consulta = db.Movimentacoes.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(filtro.Codigo))
        {
            var codigo = Produto.NormalizarCodigo(filtro.Codigo);
            consulta = consulta.Where(m => m.ProdutoCodigo == codigo);
        }
        if (filtro.Tipo is TipoMovimentacao tipo)
            consulta = consulta.Where(m => m.Tipo == tipo);
        if (filtro.DataInicio is DateOnly inicio)
        {
            var de = inicio.ToDateTime(TimeOnly.MinValue);
            consulta = consulta.Where(m => m.DataHora >= de);
        }
        if (filtro.DataFim is DateOnly fim)
        {
            var ate = fim.AddDays(1).ToDateTime(TimeOnly.MinValue);
            consulta = consulta.Where(m => m.DataHora < ate);
        }

        var total = await consulta.CountAsync();
        var itens = await consulta.OrderByDescending(m => m.Id)
                                  .Skip(filtro.Offset)
                                  .Take(filtro.Limite)
                                  .ToListAsync();
        return (total, itens);
    }

    // ==================================================================
    // Cadastro
    // ==================================================================

    public async Task<Produto> CadastrarAsync(ProdutoCriarRequest dados)
    {
        await Trava.WaitAsync();
        try
        {
            var responsavel = Movimentacao.ValidarResponsavel(dados.Responsavel);
            var produto = new Produto
            {
                Codigo = dados.Codigo ?? string.Empty,
                Nome = dados.Nome ?? string.Empty,
                Quantidade = dados.Quantidade ?? 0,
                ValorUnitario = dados.ValorUnitario ?? 0,
                PesoKg = dados.PesoKg ?? 0,
                Validade = dados.Validade,
                Corredor = dados.Endereco?.Corredor ?? string.Empty,
                Prateleira = dados.Endereco?.Prateleira ?? string.Empty,
                EstoqueMinimo = dados.EstoqueMinimo ?? 0,
                EstoqueMaximo = dados.EstoqueMaximo ?? 0
            };
            produto.Normalizar();
            produto.Validar();
            produto.ValidarValidadeNaoVencida(Hoje);

            if (produto.Quantidade > produto.EstoqueMaximo)
                throw RegraNegocioException.Validacao(
                    $"a quantidade inicial ({produto.Quantidade}) excede o estoque máximo ({produto.EstoqueMaximo})");

            var existente = await db.Produtos.AsNoTracking().FirstOrDefaultAsync(p => p.Codigo == produto.Codigo);
            if (existente is not null)
                throw RegraNegocioException.Conflito(existente.Ativo
                    ? $"já existe um produto cadastrado com o código '{produto.Codigo}'"
                    : $"o código '{produto.Codigo}' pertence a um produto removido e não pode ser reutilizado");

            produto.CriadoEm = produto.AtualizadoEm = Agora;

            await using var transacao = await db.Database.BeginTransactionAsync();
            db.Produtos.Add(produto);
            await db.SaveChangesAsync();                  // gera o Id do produto
            db.Movimentacoes.Add(NovaMovimentacao(TipoMovimentacao.Cadastro, produto, produto.Quantidade, 0,
                produto.ValorUnitario, responsavel,
                $"Produto cadastrado com saldo inicial de {produto.Quantidade} unidade(s) em {produto.EnderecoDescricao}"));
            await db.SaveChangesAsync();
            await transacao.CommitAsync();
            return produto;
        }
        finally
        {
            Trava.Release();
        }
    }

    public async Task<Produto> AtualizarAsync(string codigo, ProdutoAtualizarRequest dados)
    {
        await Trava.WaitAsync();
        try
        {
            var produto = await ObterProdutoAsync(codigo);

            if (dados.Quantidade is not null)
                throw RegraNegocioException.Validacao(
                    "a quantidade não pode ser alterada pelo cadastro; use entrada, saída ou ajuste em /api/movimentacoes");
            if (dados.Codigo is not null && Produto.NormalizarCodigo(dados.Codigo) != produto.Codigo)
                throw RegraNegocioException.Validacao("o código do produto não pode ser alterado");

            var responsavel = Movimentacao.ValidarResponsavel(dados.Responsavel);

            // Guarda os valores antigos para registrar no log o que mudou.
            var antes = new
            {
                produto.Nome, produto.ValorUnitario, produto.PesoKg, produto.Validade,
                produto.Corredor, produto.Prateleira, produto.EstoqueMinimo, produto.EstoqueMaximo
            };

            if (dados.Nome is not null) produto.Nome = dados.Nome;
            if (dados.ValorUnitario is decimal valor) produto.ValorUnitario = valor;
            if (dados.PesoKg is decimal peso) produto.PesoKg = peso;
            if (dados.RemoverValidade == true) produto.Validade = null;
            else if (dados.Validade is DateOnly validade) produto.Validade = validade;
            if (dados.Endereco?.Corredor is not null) produto.Corredor = dados.Endereco.Corredor;
            if (dados.Endereco?.Prateleira is not null) produto.Prateleira = dados.Endereco.Prateleira;
            if (dados.EstoqueMinimo is int minimo) produto.EstoqueMinimo = minimo;
            if (dados.EstoqueMaximo is int maximo) produto.EstoqueMaximo = maximo;

            produto.Normalizar();
            produto.Validar();
            if (produto.Validade != antes.Validade)
                produto.ValidarValidadeNaoVencida(Hoje);

            var mudancas = new List<string>();
            if (produto.Nome != antes.Nome)
                mudancas.Add($"nome: '{antes.Nome}' -> '{produto.Nome}'");
            if (produto.ValorUnitario != antes.ValorUnitario)
                mudancas.Add($"valor unitário: {antes.ValorUnitario:0.00} -> {produto.ValorUnitario:0.00}");
            if (produto.PesoKg != antes.PesoKg)
                mudancas.Add($"peso: {antes.PesoKg:0.###} kg -> {produto.PesoKg:0.###} kg");
            if (produto.Validade != antes.Validade)
                mudancas.Add($"validade: {TextoValidade(antes.Validade)} -> {TextoValidade(produto.Validade)}");
            if (produto.Corredor != antes.Corredor || produto.Prateleira != antes.Prateleira)
                mudancas.Add($"endereço: {antes.Corredor}/{antes.Prateleira} -> {produto.Corredor}/{produto.Prateleira}");
            if (produto.EstoqueMinimo != antes.EstoqueMinimo)
                mudancas.Add($"estoque mínimo: {antes.EstoqueMinimo} -> {produto.EstoqueMinimo}");
            if (produto.EstoqueMaximo != antes.EstoqueMaximo)
                mudancas.Add($"estoque máximo: {antes.EstoqueMaximo} -> {produto.EstoqueMaximo}");

            if (mudancas.Count == 0)
                return produto;                           // nada mudou: não gera histórico

            var descricao = string.Join("; ", mudancas);
            if (produto.Quantidade > produto.EstoqueMaximo)
                descricao += " (atenção: saldo atual acima do novo máximo)";

            produto.AtualizadoEm = Agora;
            db.Movimentacoes.Add(NovaMovimentacao(TipoMovimentacao.Edicao, produto, 0, produto.Quantidade,
                produto.ValorUnitario, responsavel, descricao));
            await db.SaveChangesAsync();
            return produto;
        }
        finally
        {
            Trava.Release();
        }
    }

    /// <summary>Exclusão lógica. Só é permitida com saldo zero, para preservar o histórico.</summary>
    public async Task RemoverAsync(string codigo, string? responsavel, string? motivo)
    {
        await Trava.WaitAsync();
        try
        {
            var produto = await ObterProdutoAsync(codigo);
            var resp = Movimentacao.ValidarResponsavel(responsavel);
            var obs = Movimentacao.ValidarObservacao(motivo, obrigatoria: false);

            if (produto.Quantidade > 0)
                throw RegraNegocioException.Conflito(
                    $"não é possível remover '{produto.Codigo}': ainda há {produto.Quantidade} unidade(s) em estoque. " +
                    "Registre a saída ou um ajuste antes de remover");

            produto.Ativo = false;
            produto.AtualizadoEm = Agora;
            db.Movimentacoes.Add(NovaMovimentacao(TipoMovimentacao.Remocao, produto, 0, 0, produto.ValorUnitario,
                resp, obs ?? "Produto removido do cadastro"));
            await db.SaveChangesAsync();
        }
        finally
        {
            Trava.Release();
        }
    }

    // ==================================================================
    // Movimentações (atualizam a quantidade em estoque)
    // ==================================================================

    public async Task<(Movimentacao Movimentacao, Produto Produto)> RegistrarEntradaAsync(EntradaRequest dados)
    {
        await Trava.WaitAsync();
        try
        {
            var produto = await ObterProdutoAsync(dados.Codigo ?? string.Empty);
            var quantidade = dados.Quantidade ?? 0;
            Movimentacao.ValidarQuantidade(quantidade);
            var responsavel = Movimentacao.ValidarResponsavel(dados.Responsavel);
            var observacao = Movimentacao.ValidarObservacao(dados.Observacao, obrigatoria: false);
            if (dados.ValorUnitario is decimal custo) Produto.ValidarValorUnitario(custo);

            if ((long)produto.Quantidade + quantidade > produto.EstoqueMaximo)
            {
                var disponivel = Math.Max(0, produto.EstoqueMaximo - produto.Quantidade);
                throw RegraNegocioException.Regra(
                    $"a entrada de {quantidade} unidade(s) ultrapassa o estoque máximo de '{produto.Codigo}' " +
                    $"(saldo atual {produto.Quantidade}, máximo {produto.EstoqueMaximo}, capacidade disponível {disponivel})");
            }

            var saldoAnterior = produto.Quantidade;
            var custoEntrada = dados.ValorUnitario ?? produto.ValorUnitario;

            // Custo médio ponderado: o valor em estoque fica coerente com o que foi pago.
            if (dados.ValorUnitario is decimal novoCusto)
                produto.ValorUnitario = Math.Round(
                    (saldoAnterior * produto.ValorUnitario + quantidade * novoCusto) / (saldoAnterior + quantidade), 2);

            produto.Quantidade += quantidade;
            produto.AtualizadoEm = Agora;

            var mov = NovaMovimentacao(TipoMovimentacao.Entrada, produto, quantidade, saldoAnterior,
                custoEntrada, responsavel, observacao);
            db.Movimentacoes.Add(mov);
            await db.SaveChangesAsync();
            return (mov, produto);
        }
        finally
        {
            Trava.Release();
        }
    }

    public async Task<(Movimentacao Movimentacao, Produto Produto)> RegistrarSaidaAsync(SaidaRequest dados)
    {
        await Trava.WaitAsync();
        try
        {
            var produto = await ObterProdutoAsync(dados.Codigo ?? string.Empty);
            var quantidade = dados.Quantidade ?? 0;
            Movimentacao.ValidarQuantidade(quantidade);
            var responsavel = Movimentacao.ValidarResponsavel(dados.Responsavel);
            var observacao = Movimentacao.ValidarObservacao(dados.Observacao, obrigatoria: false);

            if (quantidade > produto.Quantidade)
                throw RegraNegocioException.Regra(
                    $"estoque insuficiente para '{produto.Codigo}': solicitado {quantidade}, disponível {produto.Quantidade}");

            var saldoAnterior = produto.Quantidade;
            produto.Quantidade -= quantidade;
            produto.AtualizadoEm = Agora;

            var mov = NovaMovimentacao(TipoMovimentacao.Saida, produto, quantidade, saldoAnterior,
                produto.ValorUnitario, responsavel, observacao);
            db.Movimentacoes.Add(mov);
            await db.SaveChangesAsync();
            return (mov, produto);
        }
        finally
        {
            Trava.Release();
        }
    }

    /// <summary>Define o saldo para a quantidade contada no inventário (motivo obrigatório).</summary>
    public async Task<(Movimentacao Movimentacao, Produto Produto)> RegistrarAjusteAsync(AjusteRequest dados)
    {
        await Trava.WaitAsync();
        try
        {
            var produto = await ObterProdutoAsync(dados.Codigo ?? string.Empty);
            var contada = dados.QuantidadeContada ?? -1;
            if (contada < 0)
                throw RegraNegocioException.Validacao("a quantidade contada não pode ser negativa");
            if (contada > Produto.QuantidadeLimite)
                throw RegraNegocioException.Validacao($"a quantidade excede o limite de {Produto.QuantidadeLimite} unidades");
            var responsavel = Movimentacao.ValidarResponsavel(dados.Responsavel);
            var motivo = Movimentacao.ValidarObservacao(dados.Observacao, obrigatoria: true);

            if (contada == produto.Quantidade)
                throw RegraNegocioException.Validacao(
                    $"a quantidade contada é igual ao saldo atual ({produto.Quantidade}); nenhum ajuste é necessário");

            var saldoAnterior = produto.Quantidade;
            produto.Quantidade = contada;
            produto.AtualizadoEm = Agora;

            // quantidade com sinal: positiva = sobra, negativa = perda/falta
            var mov = NovaMovimentacao(TipoMovimentacao.Ajuste, produto, contada - saldoAnterior, saldoAnterior,
                produto.ValorUnitario, responsavel, motivo);
            db.Movimentacoes.Add(mov);
            await db.SaveChangesAsync();
            return (mov, produto);
        }
        finally
        {
            Trava.Release();
        }
    }

    // ==================================================================
    // Auxiliares
    // ==================================================================

    private static Movimentacao NovaMovimentacao(TipoMovimentacao tipo, Produto produto, int quantidade,
        int saldoAnterior, decimal valorUnitario, string responsavel, string? observacao)
    {
        if (observacao is { Length: > Movimentacao.ObservacaoMaxInterna })
            observacao = observacao[..Movimentacao.ObservacaoMaxInterna];

        return new Movimentacao
        {
            Tipo = tipo,
            ProdutoId = produto.Id,
            ProdutoCodigo = produto.Codigo,
            ProdutoNome = produto.Nome,
            Quantidade = quantidade,
            SaldoAnterior = saldoAnterior,
            SaldoAtual = produto.Quantidade,
            ValorUnitario = Math.Round(valorUnitario, 2),
            ValorTotal = Math.Round(quantidade * valorUnitario, 2),
            Responsavel = responsavel,
            Observacao = observacao,
            DataHora = Agora
        };
    }

    private static string TextoValidade(DateOnly? validade) =>
        validade is DateOnly data ? data.ToString("yyyy-MM-dd") : "sem validade";
}

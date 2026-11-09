using System.Globalization;
using System.Net;
using System.Text;
using StockWay.DTOs;

namespace StockWay.Services;

/// <summary>
/// Transforma o relatório diário em e-mail: HTML com estilos inline (os leitores de e-mail ignoram
/// CSS externo) nas cores do StockWay, e uma versão em texto puro para quem não exibe HTML.
/// Todo texto vindo do banco passa por HtmlEncode.
/// </summary>
public static class RelatorioDiarioEmail
{
    private static readonly CultureInfo Br = CultureInfo.GetCultureInfo("pt-BR");

    // Cores de frontend/src/assets/css/base.css
    private const string Azul900 = "#0F2A5C", Azul700 = "#1E3A8A", Azul600 = "#2563EB", AzulClaro = "#DBEAFE";
    private const string Escuro = "#0F172A", Fraco = "#64748B", Borda = "#E2E8F0", Fundo = "#F8FAFC";
    private const string Verde = "#15803D", Vermelho = "#DC2626", VermelhoClaro = "#FEE2E2", Ambar = "#B45309", AmbarClaro = "#FEF3C7";

    public static (string Assunto, string Html, string Texto) Montar(RelatorioDiarioResponse r, string rodape)
    {
        var data = r.Data.ToString("dd/MM/yyyy", Br);
        var periodo = Periodo(r.PeriodoInicio, r.PeriodoFim);
        var assunto = $"Relatório do estoque de {r.Data.ToString("dd/MM", Br)}: " +
                      $"{Contar(r.NoEstoqueMinimo.Count, "produto", "produtos")} no estoque mínimo e " +
                      $"{Contar(r.VencemEmBreve.Count, "vencendo", "vencendo")} em até {r.DiasAvisoValidade} dias";
        var previa = $"{Contar(r.Entradas.Registros, "entrada", "entradas")} e {Contar(r.Saidas.Registros, "saída", "saídas")} {periodo}.";

        return (assunto, MontarHtml(r, assunto, previa, data, periodo, rodape), MontarTexto(r, data, periodo, rodape));
    }

    // ------------------------------------------------------------------
    // HTML
    // ------------------------------------------------------------------

    private static string MontarHtml(RelatorioDiarioResponse r, string assunto, string previa, string data, string periodo, string rodape)
    {
        var h = new StringBuilder();
        h.Append($"""
            <!doctype html>
            <html lang="pt-BR">
            <head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>{E(assunto)}</title></head>
            <body style="margin:0;padding:0;background:{Fundo};font-family:'Segoe UI',Roboto,Helvetica,Arial,sans-serif;color:{Escuro};">
            <div style="display:none;max-height:0;overflow:hidden;opacity:0;">{E(previa)}</div>
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="background:{Fundo};">
            <tr><td align="center" style="padding:24px 12px;">
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:640px;background:#FFFFFF;border:1px solid {Borda};border-radius:12px;">
            <tr><td style="background:{Azul700};background-image:linear-gradient(160deg,{Azul700},{Azul900});border-radius:12px 12px 0 0;padding:24px 28px;">
              <div style="font-size:14px;line-height:20px;font-weight:600;color:{AzulClaro};">StockWay</div>
              <div style="font-size:22px;line-height:30px;font-weight:600;color:#FFFFFF;margin-top:4px;">Relatório do estoque de {data}</div>
              <div style="font-size:13px;line-height:20px;color:{AzulClaro};margin-top:6px;">Movimentações {E(periodo)}</div>
            </td></tr>
            <tr><td style="padding:4px 28px 24px;">
            """);

        // ---- Entradas e saídas ----
        h.Append(Titulo("Entradas e saídas"));
        h.Append($"""
            <table role="presentation" width="100%" cellpadding="0" cellspacing="0"><tr>
              <td width="50%" valign="top" style="padding-right:6px;">{Cartao("Entradas", r.Entradas, "recebidas", Verde)}</td>
              <td width="50%" valign="top" style="padding-left:6px;">{Cartao("Saídas", r.Saidas, "retiradas", Azul600)}</td>
            </tr></table>
            """);
        if (r.MovimentacoesPorProduto.Count > 0)
        {
            h.Append(Tabela(["Produto", "Entrou", "Saiu"], [false, true, true],
                r.MovimentacoesPorProduto.Select(m => new[]
                {
                    Produto(m.Nome, m.Codigo),
                    m.UnidadesEntrada > 0 ? Cor($"+{N(m.UnidadesEntrada)}", Verde) : Cor("0", Fraco),
                    m.UnidadesSaida > 0 ? Cor($"−{N(m.UnidadesSaida)}", Azul600) : Cor("0", Fraco)
                })));
        }

        // ---- Validade ----
        h.Append(Titulo($"Vencem em até {r.DiasAvisoValidade} dias", r.VencemEmBreve.Count));
        if (r.VencemEmBreve.Count == 0)
            h.Append(Vazio($"Nenhum produto com saldo vence nos próximos {r.DiasAvisoValidade} dias."));
        else
            h.Append(Tabela(["Produto", "Validade", "Situação", "Saldo", "Endereço"], [false, false, false, true, false],
                r.VencemEmBreve.Select(p => new[]
                {
                    Produto(p.Nome, p.Codigo),
                    E(p.Validade?.ToString("dd/MM/yyyy", Br) ?? ""),
                    Selo(SituacaoDaValidade(p.DiasParaVencer), p.DiasParaVencer < 0 ? Vermelho : Ambar, p.DiasParaVencer < 0 ? VermelhoClaro : AmbarClaro),
                    N(p.Quantidade),
                    Endereco(p.Endereco)
                })));

        // ---- Estoque mínimo ----
        h.Append(Titulo("No estoque mínimo ou abaixo", r.NoEstoqueMinimo.Count));
        if (r.NoEstoqueMinimo.Count == 0)
            h.Append(Vazio("Nenhum produto está no estoque mínimo."));
        else
            h.Append(Tabela(["Produto", "Saldo", "Mínimo", "Repor", "Endereço"], [false, true, true, true, false],
                r.NoEstoqueMinimo.Select(p => new[]
                {
                    Produto(p.Nome, p.Codigo),
                    p.Quantidade == 0 ? Cor("0", Vermelho, negrito: true) : Cor(N(p.Quantidade), Vermelho),
                    N(p.EstoqueMinimo),
                    N(p.QuantidadeParaRepor),
                    Endereco(p.Endereco)
                })));

        h.Append($"""
            </td></tr>
            <tr><td style="padding:16px 28px;background:{Fundo};border-top:1px solid {Borda};border-radius:0 0 12px 12px;font-size:12px;line-height:18px;color:{Fraco};">{E(rodape)}</td></tr>
            </table>
            </td></tr>
            </table>
            </body>
            </html>
            """);
        return h.ToString();
    }

    private static string Titulo(string texto, int? quantidade = null) =>
        $"""<h2 style="font-size:16px;line-height:24px;font-weight:600;color:{Escuro};margin:28px 0 12px;">{E(texto)}{(quantidade is int q ? $" <span style=\"color:{Fraco};font-weight:400;\">({N(q)})</span>" : "")}</h2>""";

    private static string Cartao(string rotulo, TotaisDoPeriodo totais, string verbo, string cor) => $"""
        <div style="border:1px solid {Borda};border-top:3px solid {cor};border-radius:10px;padding:12px 16px;">
          <div style="font-size:13px;line-height:20px;color:{Fraco};">{rotulo}</div>
          <div style="font-size:28px;line-height:36px;font-weight:700;color:{cor};">{N(totais.Registros)}</div>
          <div style="font-size:13px;line-height:20px;color:{Fraco};">{Contar(totais.Unidades, "unidade", "unidades")} {verbo}</div>
        </div>
        """;

    private static string Tabela(string[] cabecalhos, bool[] direita, IEnumerable<string[]> linhas)
    {
        var t = new StringBuilder();
        t.Append($"""<table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="border-collapse:collapse;font-size:13px;line-height:18px;margin-top:12px;">""");
        t.Append("<tr>");
        for (var i = 0; i < cabecalhos.Length; i++)
            t.Append($"""<th style="text-align:{(direita[i] ? "right" : "left")};font-weight:500;color:{Fraco};padding:8px 8px;border-bottom:1px solid {Borda};background:{Fundo};">{E(cabecalhos[i])}</th>""");
        t.Append("</tr>");
        foreach (var linha in linhas)
        {
            t.Append("<tr>");
            for (var i = 0; i < linha.Length; i++)
                t.Append($"""<td style="text-align:{(direita[i] ? "right" : "left")};padding:9px 8px;border-bottom:1px solid {Borda};vertical-align:top;">{linha[i]}</td>""");
            t.Append("</tr>");
        }
        t.Append("</table>");
        return t.ToString();
    }

    private static string Produto(string nome, string codigo) =>
        $"""<span style="font-weight:500;color:{Escuro};">{E(nome)}</span><br><span style="font-size:12px;color:{Fraco};">{E(codigo)}</span>""";

    private static string Endereco(EnderecoResponse e) =>
        $"""<span style="white-space:nowrap;">{E(e.Corredor)} / {E(e.Prateleira)}</span>""";

    private static string Selo(string texto, string cor, string fundo) =>
        $"""<span style="display:inline-block;white-space:nowrap;padding:1px 8px;border-radius:999px;background:{fundo};color:{cor};font-weight:500;">{E(texto)}</span>""";

    private static string Cor(string texto, string cor, bool negrito = false) =>
        $"""<span style="color:{cor};{(negrito ? "font-weight:700;" : "")}">{E(texto)}</span>""";

    private static string Vazio(string texto) =>
        $"""<p style="margin:0;padding:12px 14px;border-radius:8px;background:{Fundo};color:{Fraco};font-size:13px;line-height:20px;">{E(texto)}</p>""";

    // ------------------------------------------------------------------
    // Texto puro
    // ------------------------------------------------------------------

    private static string MontarTexto(RelatorioDiarioResponse r, string data, string periodo, string rodape)
    {
        var t = new StringBuilder();
        t.AppendLine($"StockWay - Relatório do estoque de {data}");
        t.AppendLine($"Movimentações {periodo}");
        t.AppendLine();
        t.AppendLine("ENTRADAS E SAÍDAS");
        t.AppendLine($"Entradas: {N(r.Entradas.Registros)} ({Contar(r.Entradas.Unidades, "unidade", "unidades")})");
        t.AppendLine($"Saídas: {N(r.Saidas.Registros)} ({Contar(r.Saidas.Unidades, "unidade", "unidades")})");
        foreach (var m in r.MovimentacoesPorProduto)
            t.AppendLine($"  {m.Nome} ({m.Codigo}): entrou {N(m.UnidadesEntrada)}, saiu {N(m.UnidadesSaida)}");
        t.AppendLine();

        t.AppendLine($"VENCEM EM ATÉ {r.DiasAvisoValidade} DIAS ({r.VencemEmBreve.Count})");
        if (r.VencemEmBreve.Count == 0) t.AppendLine("Nenhum produto com saldo vence nesse prazo.");
        foreach (var p in r.VencemEmBreve)
            t.AppendLine($"  {p.Nome} ({p.Codigo}): {SituacaoDaValidade(p.DiasParaVencer)}, validade {p.Validade?.ToString("dd/MM/yyyy", Br)}, saldo {N(p.Quantidade)}, {p.Endereco.Descricao}");
        t.AppendLine();

        t.AppendLine($"NO ESTOQUE MÍNIMO OU ABAIXO ({r.NoEstoqueMinimo.Count})");
        if (r.NoEstoqueMinimo.Count == 0) t.AppendLine("Nenhum produto está no estoque mínimo.");
        foreach (var p in r.NoEstoqueMinimo)
            t.AppendLine($"  {p.Nome} ({p.Codigo}): saldo {N(p.Quantidade)}, mínimo {N(p.EstoqueMinimo)}, repor {N(p.QuantidadeParaRepor)}, {p.Endereco.Descricao}");
        t.AppendLine();
        t.AppendLine(rodape);
        return t.ToString();
    }

    // ------------------------------------------------------------------
    // Auxiliares
    // ------------------------------------------------------------------

    private static string Periodo(DateTime inicio, DateTime fim) =>
        inicio.Date == fim.Date
            ? $"de {fim.ToString("dd/MM", Br)}, das {inicio:HH:mm} às {fim:HH:mm}"
            : $"de {inicio.ToString("dd/MM", Br)} às {inicio:HH:mm} até {fim.ToString("dd/MM", Br)} às {fim:HH:mm}";

    private static string SituacaoDaValidade(int? dias) => dias switch
    {
        null => "sem validade",
        < -1 => $"vencido há {-dias} dias",
        -1 => "venceu ontem",
        0 => "vence hoje",
        1 => "vence amanhã",
        _ => $"vence em {dias} dias"
    };

    private static string N(int valor) => valor.ToString("N0", Br);

    private static string Contar(int quantidade, string singular, string plural) =>
        $"{N(quantidade)} {(quantidade == 1 ? singular : plural)}";

    private static string E(string texto) => WebUtility.HtmlEncode(texto);
}

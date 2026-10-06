/*
 * strockway-api.js - Cliente JavaScript para consumir o back-end do StrockWay no front-end.
 *
 * Copie este arquivo para o seu projeto do site e importe:
 *     import { api } from "./strockway-api.js";
 *     const dash = await api.dashboard();
 *
 * Todas as funções retornam o conteúdo de "dados" da resposta.
 * Em caso de erro, lançam um Error com a mensagem enviada pela API
 * (ex.: "estoque insuficiente para 'PAR-001': solicitado 10, disponível 4").
 */

export const API_URL = "http://localhost:8080/api";

async function requisicao(metodo, caminho, corpo) {
  const opcoes = { method: metodo, headers: {} };
  if (corpo !== undefined) {
    opcoes.headers["Content-Type"] = "application/json";
    opcoes.body = JSON.stringify(corpo);
  }

  const resposta = await fetch(API_URL + caminho, opcoes);
  if (resposta.status === 204) return null;

  const json = await resposta.json();
  if (!json.sucesso) {
    const erro = new Error(json.erro || "erro desconhecido");
    erro.status = resposta.status;
    throw erro;
  }
  return json.dados;
}

/* Monta "?a=1&b=2" ignorando valores vazios. */
function query(parametros = {}) {
  const qs = new URLSearchParams();
  for (const [chave, valor] of Object.entries(parametros)) {
    if (valor !== undefined && valor !== null && valor !== "") qs.append(chave, valor);
  }
  const texto = qs.toString();
  return texto ? "?" + texto : "";
}

const cod = (codigo) => encodeURIComponent(codigo);

export const api = {
  // ---------- Geral ----------
  health:     ()            => requisicao("GET", "/health"),
  dashboard:  ()            => requisicao("GET", "/dashboard"),

  // ---------- Produtos ----------
  // filtros: { busca, corredor, status, validade, ordenar, incluir_inativos }
  listarProdutos:  (filtros) => requisicao("GET", "/produtos" + query(filtros)),
  obterProduto:    (codigo)  => requisicao("GET", `/produtos/${cod(codigo)}`),
  cadastrarProduto:(produto) => requisicao("POST", "/produtos", produto),
  atualizarProduto:(codigo, campos) => requisicao("PUT", `/produtos/${cod(codigo)}`, campos),
  removerProduto:  (codigo, responsavel, motivo) =>
    requisicao("DELETE", `/produtos/${cod(codigo)}`, { responsavel, motivo }),
  historicoProduto:(codigo, filtros) =>
    requisicao("GET", `/produtos/${cod(codigo)}/movimentacoes` + query(filtros)),

  // ---------- Movimentações ----------
  registrarEntrada: (dados) => requisicao("POST", "/movimentacoes/entrada", dados),
  registrarSaida:   (dados) => requisicao("POST", "/movimentacoes/saida", dados),
  registrarAjuste:  (dados) => requisicao("POST", "/movimentacoes/ajuste", dados),
  // filtros: { codigo, tipo, data_inicio, data_fim, limite, offset }
  listarMovimentacoes: (filtros) => requisicao("GET", "/movimentacoes" + query(filtros)),

  // ---------- Alertas ----------
  baixoEstoque: ()          => requisicao("GET", "/estoque/baixo"),
  alertas:      ()          => requisicao("GET", "/estoque/alertas"),
  validade:     (dias = 30) => requisicao("GET", "/estoque/validade" + query({ dias })),
};

/* ------------------------------------------------------------------
 * Exemplos de uso
 * ------------------------------------------------------------------
 *
 * // Cadastro (formulário)
 * try {
 *   const produto = await api.cadastrarProduto({
 *     codigo: "LUV-010",
 *     nome: "Luva nitrílica (par)",
 *     quantidade: 50,
 *     valor_unitario: 4.9,
 *     peso_kg: 0.03,
 *     validade: "2027-06-30",          // opcional; omita se não for perecível
 *     endereco: { corredor: "B", prateleira: "03" },
 *     estoque_minimo: 30,
 *     estoque_maximo: 300,
 *     responsavel: "Maria Souza",
 *   });
 *   alert("Cadastrado! Valor em estoque: R$ " + produto.valor_em_estoque);
 * } catch (e) {
 *   alert(e.message);                  // mensagem de validação vinda do back-end
 * }
 *
 * // Saída de material
 * const r = await api.registrarSaida({
 *   codigo: "LUV-010", quantidade: 5, responsavel: "João", observacao: "OS 123",
 * });
 * r.alertas.forEach((a) => console.warn(a));   // ex.: "produto atingiu o estoque mínimo..."
 *
 * // Tabela do log de movimentações (página 2, 20 por página)
 * const log = await api.listarMovimentacoes({ limite: 20, offset: 20 });
 * log.itens.forEach((m) => console.log(m.data_hora, m.tipo_descricao, m.produto_nome, m.quantidade));
 */

import { requisicao } from './http'
import { nomeDoResponsavel } from '../stores/sessao'

export function listarProdutos(filtros) {
  return requisicao('GET', '/produtos', { parametros: filtros })
}

export function buscarProduto(codigo) {
  return requisicao('GET', `/produtos/${encodeURIComponent(codigo)}`)
}

export function cadastrarProduto(produto) {
  return requisicao('POST', '/produtos', { corpo: { ...produto, responsavel: nomeDoResponsavel() } })
}

export function editarProduto(codigo, campos) {
  return requisicao('PUT', `/produtos/${encodeURIComponent(codigo)}`, {
    corpo: { ...campos, responsavel: nomeDoResponsavel() }
  })
}

export function removerProduto(codigo, motivo) {
  return requisicao('DELETE', `/produtos/${encodeURIComponent(codigo)}`, {
    corpo: { responsavel: nomeDoResponsavel(), motivo }
  })
}

export function historicoDoProduto(codigo, filtros) {
  return requisicao('GET', `/produtos/${encodeURIComponent(codigo)}/movimentacoes`, { parametros: filtros })
}

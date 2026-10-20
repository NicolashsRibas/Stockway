import { requisicao } from './http'
import { nomeDoResponsavel } from '../stores/sessao'

export function listarMovimentacoes(filtros) {
  return requisicao('GET', '/movimentacoes', { parametros: filtros })
}

export function registrarEntrada(dados) {
  return requisicao('POST', '/movimentacoes/entrada', { corpo: { ...dados, responsavel: nomeDoResponsavel() } })
}

export function registrarSaida(dados) {
  return requisicao('POST', '/movimentacoes/saida', { corpo: { ...dados, responsavel: nomeDoResponsavel() } })
}

export function registrarAjuste(dados) {
  return requisicao('POST', '/movimentacoes/ajuste', { corpo: { ...dados, responsavel: nomeDoResponsavel() } })
}

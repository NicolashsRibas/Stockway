import { requisicao } from './http'

export function carregarDashboard() {
  return requisicao('GET', '/dashboard')
}

export function produtosComEstoqueBaixo() {
  return requisicao('GET', '/estoque/baixo')
}

export function verificarApi() {
  return requisicao('GET', '/health')
}

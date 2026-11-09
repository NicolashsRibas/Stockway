import { requisicao } from './http'

export function fazerLogin(email, senha) {
  return requisicao('POST', '/auth/login', { corpo: { email, senha } })
}

export function criarConta({ nome, cargo, email, senha }) {
  return requisicao('POST', '/auth/cadastro', { corpo: { nome, cargo, email, senha } })
}

export function usuarioAtual() {
  return requisicao('GET', '/auth/eu')
}

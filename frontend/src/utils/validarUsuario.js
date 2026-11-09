// Mesmas regras do Models/Usuario.cs da API. O front confere antes de enviar para o erro
// aparecer embaixo do campo na hora; mesmo assim, quem decide é a API.

export const LIMITES_DO_USUARIO = {
  nomeMinimo: 2,
  nomeMaximo: 60,
  cargoMinimo: 2,
  cargoMaximo: 60,
  emailMaximo: 255,
  senhaMinima: 8,
  senhaMaxima: 128
}

const EMAIL_VALIDO = /^[^@\s]+@[^@\s]+\.[^@\s]+$/
const CARACTERE_DE_CONTROLE = /\p{Cc}/u

export function regrasDaSenha(senha) {
  const texto = String(senha ?? '')
  return [
    {
      texto: `De ${LIMITES_DO_USUARIO.senhaMinima} a ${LIMITES_DO_USUARIO.senhaMaxima} caracteres`,
      ok: texto.length >= LIMITES_DO_USUARIO.senhaMinima && texto.length <= LIMITES_DO_USUARIO.senhaMaxima
    },
    { texto: 'Pelo menos uma letra', ok: /\p{L}/u.test(texto) },
    { texto: 'Pelo menos um número', ok: /\p{Nd}/u.test(texto) }
  ]
}

function validarEmail(email) {
  const texto = String(email ?? '').trim()
  if (!texto) return 'Informe o seu e-mail.'
  if (texto.length > LIMITES_DO_USUARIO.emailMaximo) return `Use no máximo ${LIMITES_DO_USUARIO.emailMaximo} caracteres.`
  if (!EMAIL_VALIDO.test(texto)) return 'Use um e-mail como nome@empresa.com.br.'
  return ''
}

function validarTexto(valor, minimo, maximo, vazio) {
  const texto = String(valor ?? '').trim()
  if (!texto) return vazio
  if (CARACTERE_DE_CONTROLE.test(texto)) return 'Use apenas letras, números e pontuação comum.'
  if (texto.length < minimo || texto.length > maximo) return `Use de ${minimo} a ${maximo} caracteres.`
  return ''
}

function juntar(erros) {
  return Object.fromEntries(Object.entries(erros).filter(([, mensagem]) => mensagem))
}

export function validarLogin(form) {
  return juntar({
    email: validarEmail(form.email),
    senha: form.senha ? '' : 'Informe a sua senha.'
  })
}

export function validarCadastro(form) {
  const senhaOk = regrasDaSenha(form.senha).every((regra) => regra.ok)
  return juntar({
    nome: validarTexto(form.nome, LIMITES_DO_USUARIO.nomeMinimo, LIMITES_DO_USUARIO.nomeMaximo, 'Informe o seu nome.'),
    cargo: validarTexto(form.cargo, LIMITES_DO_USUARIO.cargoMinimo, LIMITES_DO_USUARIO.cargoMaximo, 'Informe o seu cargo.'),
    email: validarEmail(form.email),
    senha: !form.senha ? 'Crie uma senha.' : senhaOk ? '' : 'A senha ainda não cumpre as regras abaixo.'
  })
}

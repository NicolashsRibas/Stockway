const formatoNumero = new Intl.NumberFormat('pt-BR')
const formatoDinheiro = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })
const formatoPeso = new Intl.NumberFormat('pt-BR', { maximumFractionDigits: 3 })
const formatoDataHora = new Intl.DateTimeFormat('pt-BR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit'
})
const formatoData = new Intl.DateTimeFormat('pt-BR', { day: '2-digit', month: '2-digit', year: 'numeric' })
const formatoHoje = new Intl.DateTimeFormat('pt-BR', { weekday: 'long', day: 'numeric', month: 'long' })

export function formatarNumero(valor) {
  return formatoNumero.format(valor ?? 0)
}

export function formatarDinheiro(valor) {
  return formatoDinheiro.format(valor ?? 0)
}

export function formatarPeso(valor) {
  return `${formatoPeso.format(valor ?? 0)} kg`
}

export function formatarDataHora(data) {
  return formatoDataHora.format(new Date(data))
}

export function formatarData(dataSemHora) {
  if (!dataSemHora) return '—'
  const [ano, mes, dia] = dataSemHora.split('-').map(Number)
  return formatoData.format(new Date(ano, mes - 1, dia))
}

export function dataDeHoje() {
  const texto = formatoHoje.format(new Date())
  return texto.charAt(0).toUpperCase() + texto.slice(1)
}

export function hojeNoFormatoDaApi(somarDias = 0) {
  const data = new Date()
  data.setDate(data.getDate() + somarDias)
  const mes = String(data.getMonth() + 1).padStart(2, '0')
  const dia = String(data.getDate()).padStart(2, '0')
  return `${data.getFullYear()}-${mes}-${dia}`
}

export function semAcento(texto) {
  return String(texto ?? '')
    .normalize('NFD')
    .replace(/[̀-ͯ]/g, '')
    .toLowerCase()
}

export function primeiroNome(nome) {
  return String(nome ?? '').trim().split(' ')[0]
}

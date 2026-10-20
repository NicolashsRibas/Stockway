const CORES = [
  { cor: '#2563EB', fundo: '#DBEAFE' },
  { cor: '#7C3AED', fundo: '#EDE9FE' },
  { cor: '#EA580C', fundo: '#FFEDD5' },
  { cor: '#0891B2', fundo: '#CFFAFE' },
  { cor: '#16A34A', fundo: '#DCFCE7' },
  { cor: '#DB2777', fundo: '#FCE7F3' },
  { cor: '#4F46E5', fundo: '#E0E7FF' },
  { cor: '#0D9488', fundo: '#CCFBF1' }
]

export function coresDoCorredor(corredor) {
  const texto = String(corredor ?? '')
  const soma = [...texto].reduce((total, letra) => total + letra.charCodeAt(0), 0)
  return CORES[soma % CORES.length]
}

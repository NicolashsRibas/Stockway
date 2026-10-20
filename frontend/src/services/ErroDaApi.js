export class ErroDaApi extends Error {
  constructor(mensagem, status = 0) {
    super(mensagem)
    this.name = 'ErroDaApi'
    this.status = status
  }
}

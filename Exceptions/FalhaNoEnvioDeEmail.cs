namespace StockWay.Exceptions;

/// <summary>O serviço de e-mail (Brevo) não aceitou a mensagem ou não respondeu.</summary>
public class FalhaNoEnvioDeEmail(string mensagem) : Exception(mensagem);

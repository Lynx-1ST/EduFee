namespace K26_DotNet.Services;

public interface IPaymentGateway
{
    string Provider { get; }

    Task<PaymentSession> CreatePaymentAsync(
        PaymentGatewayRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentGatewayStatus> QueryPaymentAsync(
        string orderId,
        CancellationToken cancellationToken = default);
}

namespace NexaEcommerce.SharedKernel.Abstractions;

public interface ICurrentTenant
{
    string Id { get; }

    bool IsMultiTenant { get; }
}
using ReqLens.Domain;

namespace ReqLens.Application;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken cancellationToken);
}

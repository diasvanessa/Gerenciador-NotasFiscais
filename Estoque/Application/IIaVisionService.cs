namespace Estoque.Application;

public interface IIaVisionService
{
    Task<string> ReconhecerImagemAsync(Stream imageStream, string contentType, string fileName);
}

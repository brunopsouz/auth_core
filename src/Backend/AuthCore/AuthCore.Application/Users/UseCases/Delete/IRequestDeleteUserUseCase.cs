namespace AuthCore.Application.Users.UseCases.Delete;

/// <summary>
/// Define operação para excluir o usuário autenticado.
/// </summary>
public interface IRequestDeleteUserUseCase
{
    /// <summary>
    /// Operação para excluir o usuário autenticado.
    /// </summary>
    Task Execute();
}

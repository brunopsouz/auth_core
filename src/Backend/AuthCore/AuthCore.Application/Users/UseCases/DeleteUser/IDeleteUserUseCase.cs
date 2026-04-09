namespace AuthCore.Application.Users.UseCases.DeleteUser;

/// <summary>
/// Define operação para excluir o usuário autenticado.
/// </summary>
public interface IDeleteUserUseCase
{
    /// <summary>
    /// Operação para excluir o usuário autenticado.
    /// </summary>
    /// <param name="command">Comando da exclusão do usuário autenticado.</param>
    Task Execute(DeleteUserCommand command);
}

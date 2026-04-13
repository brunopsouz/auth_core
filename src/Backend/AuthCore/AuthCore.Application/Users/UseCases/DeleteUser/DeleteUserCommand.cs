namespace AuthCore.Application.Users.UseCases.DeleteUser;

/// <summary>
/// Representa comando para excluir o usuário autenticado.
/// </summary>
public sealed class DeleteUserCommand
{
    /// <summary>
    /// Identificador público do usuário autenticado.
    /// </summary>
    public Guid UserIdentifier { get; set; }
}

using AuthCore.Domain.Users.Aggregates;

namespace AuthCore.Domain.Users.Repositories;

/// <summary>
/// Define operações de persistência de escrita para usuário.
/// </summary>
public interface IUserRepository
{
    /// <summary>
    /// Operação para adicionar um usuário.
    /// </summary>
    /// <param name="user">Usuário a ser persistido.</param>
    Task AddAsync(User user);

    /// <summary>
    /// Operação para obter um usuário pelo identificador público.
    /// </summary>
    /// <param name="userIdentifier">Identificador público do usuário.</param>
    /// <returns>Usuário encontrado ou nulo.</returns>
    Task<User?> GetByUserIdentifierAsync(Guid userIdentifier);

    /// <summary>
    /// Operação para obter um usuário pelo e-mail.
    /// </summary>
    /// <param name="email">E-mail do usuário.</param>
    /// <returns>Usuário encontrado ou nulo.</returns>
    Task<User?> GetByEmailAsync(string email);

    /// <summary>
    /// Operação para atualizar um usuário.
    /// </summary>
    /// <param name="user">Usuário a ser atualizado.</param>
    Task UpdateAsync(User user);

    /// <summary>
    /// Operação para remover um usuário.
    /// </summary>
    /// <param name="user">Usuário a ser removido.</param>
    Task DeleteAsync(User user);
}

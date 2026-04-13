using AuthCore.Domain.Common.Exceptions;
using AuthCore.Domain.Common.Repositories;
using AuthCore.Domain.Users.Repositories;

namespace AuthCore.Application.Users.UseCases.UpdateUser;

/// <summary>
/// Representa caso de uso para atualizar o perfil do usuário autenticado.
/// </summary>
public sealed class UpdateUserUseCase : IUpdateUserUseCase
{
    private readonly IUserReadRepository _userReadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserRepository _userRepository;

    #region Constructors

    /// <summary>
    /// Operação para criar instância da classe.
    /// </summary>
    /// <param name="userRepository">Repositório de escrita de usuário.</param>
    /// <param name="userReadRepository">Repositório de leitura de usuário.</param>
    /// <param name="unitOfWork">Unidade de trabalho transacional.</param>
    public UpdateUserUseCase(
        IUserRepository userRepository,
        IUserReadRepository userReadRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _userReadRepository = userReadRepository;
        _unitOfWork = unitOfWork;
    }

    #endregion

    /// <summary>
    /// Operação para atualizar o perfil do usuário autenticado.
    /// </summary>
    /// <param name="command">Comando com os dados da atualização.</param>
    public async Task Execute(UpdateUserCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        var user = await _userReadRepository.GetByUserIdentifierAsync(command.UserIdentifier);

        if (user is null || !user.IsActive)
            throw new DomainException("Usuário não encontrado.");

        user.UpdateProfile(command.FirstName, command.LastName, command.Contact);

        await _unitOfWork.BeginTransactionAsync();

        try
        {
            await _userRepository.UpdateAsync(user);
            await _unitOfWork.CommitAsync();
        }
        catch
        {
            await _unitOfWork.RollbackAsync();
            throw;
        }
    }
}

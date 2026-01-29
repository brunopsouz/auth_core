namespace AuthCore.Domain.Entities;

/// <summary>
/// Entidade para contas de usuário.
/// </summary>
public sealed class User : EntityBase
{
    /// <summary>
    /// Primeiro nome do usuário.
    /// </summary>
    public string FirstName { get; private set; } = null!;
    
    /// <summary>
    /// Sobrenome do usuário.
    /// </summary>
    public string LastName { get; private set; } = null!;

    /// <summary>
    /// Nome completo do usuário.
    /// </summary>
    public string FullName { get; private set; } = null!;
    
    /// <summary>
    /// E-mail do usuário.
    /// </summary>
    public Email Email { get; private set; } = null!;
    
    /// <summary>
    /// Número de telefone/celular do usuário.
    /// </summary>
    public string Contact { get; private set; } = null!;
    
    /// <summary>
    /// Identificador como uma segurança adicional do usuário ao ser utilizado no claims do token.
    /// </summary>
    public Guid UserIdentifier { get; private set; }

    /// <summary>
    /// Perfis de usuário.
    /// </summary>
    public Role Role { get; private set; }

    /// <summary>
    /// Construtor vazio.
    /// </summary>
    private User() {}

    /// <summary>
    /// Construtor de inicialização dos parametros.
    /// </summary>
    /// <param name="firstName">Primeiro nome do usuário.</param>
    /// <param name="lastName">Sobrenome do usuário.</param>
    /// <param name="fullName">Nome completo do usuário.</param>
    /// <param name="email">E-mail do usuário.</param>
    /// <param name="contact">Número de telefone/celular do usuário.</param>
    /// <param name="role">Perfis de usuário.</param>
    private User(
        string firstName,
        string lastName,
        string fullName,
        Email email,
        string contact,
        Role role
    )
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        FullName = fullName;
        Email = email;
        Contact = contact;
        Role = role;
        UserIdentifier = Guid.NewGuid();
        Validate();
    }

    /// <summary>
    /// Construtor para criação de um novo usuário.
    /// </summary>
    /// <param name="firstName">Primeiro nome do usuário</param>
    /// <param name="lastName">Sobrenome do usuário.</param>
    /// <param name="fullName">Nome completo do usuário.</param>
    /// <param name="email">E-mail do usuário.</param>
    /// <param name="contact">Número de contato do usuário.</param>
    /// <param name="role">Perfis de usuário.</param>
    /// <returns></returns>
    public static User Create(
        string firstName,
        string lastName,
        string fullName,
        string email,
        string contact, 
        Role role
    )
    {
        return new User(
            firstName,
            lastName,
            fullName,
            Email.Create(email),
            contact,
            role
        );
    }
    
    /// <summary>
    /// Construtor para leitura do objeto User.
    /// </summary>
    /// <param name="firstName">Primeiro nome do usuário</param>
    /// <param name="lastName">Sobrenome do usuário.</param>
    /// <param name="fullName">Nome completo do usuário.</param>
    /// <param name="email">E-mail do usuário.</param>
    /// <param name="contact">Número de contato do usuário.</param>
    /// <param name="role">Perfis de usuário.</param>
    /// <returns></returns>
    public static User Read(
        string firstName,
        string lastName,
        string fullName,
        string email,
        string contact,
        Role role
    )
    {
        return new User(
            firstName,
            lastName,
            fullName,
            Email.Create(email),
            contact,
            role
        );
    }

    /// <summary>
    /// Valida os dados essenciais do usuário.
    /// </summary>
    private void Validate()
    {
        if(string.IsNullOrWhiteSpace(FirstName))
            throw new Exception("O nome é obrigatório.");

        if (string.IsNullOrWhiteSpace(LastName))
            throw new Exception("O sobrenome é obrigatório.");

        if (Email is null)
            throw new Exception("O e-mail é obrigatório.");

         if (!Enum.IsDefined(typeof(Role), Role))
            throw new Exception("Perfil de usuário inválido.");

    }


}
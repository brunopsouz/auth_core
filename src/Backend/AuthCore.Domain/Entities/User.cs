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
    public string Email { get; private set; } = null!;
    
    /// <summary>
    /// Número de telefone/celular do usuário.
    /// </summary>
    public string Contact { get; private set; } = null!;
    
    /// <summary>
    /// Identificador como uma segurança adicional do usuário ao ser utilizado no claims do token.
    /// </summary>
    public Guid UserIdentifier { get; private set; }

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
    private User(
        string firstName,
        string lastName,
        string fullName,
        string email,
        string contact
    )
    {
        FirstName = firstName;
        LastName = lastName;
        FullName = fullName;
        Email = email;
        Contact = contact;

        UserIdentifier = Guid.NewGuid();

    }

    /// <summary>
    /// Construtor para criação de um novo usuário.
    /// </summary>
    /// <param name="firstName">Primeiro nome do usuário</param>
    /// <param name="lastName">Sobrenome do usuário.</param>
    /// <param name="fullName">Nome completo do usuário.</param>
    /// <param name="email">E-mail do usuário.</param>
    /// <param name="contact">Número de contato do usuário.</param>
    /// <returns></returns>
    public static User Create(
        string firstName,
        string lastName,
        string fullName,
        string email,
        string contact
    )
    {
        return new User(
            firstName,
            lastName,
            fullName,
            email,
            contact
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
    /// <returns></returns>
    public static User Read(
        string firstName,
        string lastName,
        string fullName,
        string email,
        string contact
    )
    {
        return new User(
            firstName,
            lastName,
            fullName,
            email,
            contact
        );
    }

}
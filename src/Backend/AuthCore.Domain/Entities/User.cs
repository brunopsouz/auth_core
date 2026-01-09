namespace AuthCore.Domain.Entities;

/// <summary>
/// 
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
    /// 
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
    
}
namespace AuthCore.Domain.Entities; 

/// <summary>
/// Classe para entidade base contendo propriedades em comum entre todas entidades.
/// </summary>
public abstract class EntityBase
{
    /// <summary>
    /// Identificador único.
    /// </summary>
    public Guid Id { get; protected set; }

    /// <summary>
    /// Status para regra de softdelete no banco como ativo visível true ou false.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Data de criação dos dados.
    /// </summary>
    public DateTime CreatedAt { get; protected set; }
    
    /// <summary>
    /// Data de alteração dos dados.
    /// </summary>
    public DateTime UpdateAt { get; protected set; }

    /// <summary>
    /// Inicializa a entidade base definindo explicitamente a data de criação em UTC.
    /// </summary>
    /// <param name="createdAt">Data e hora de criação em UTC.</param>
    /// <param name="updateAt">Data e hora de criação em UTC.</param>
    protected EntityBase(
        DateTime createdAt,
        DateTime updateAt)
    {
        Id = Guid.NewGuid();
        CreatedAt = createdAt;
        UpdateAt = updateAt;
    }

    /// <summary>
    /// Construtor protegido sem parâmetros.
    /// </summary>
    protected EntityBase() { }

    /// <summary>
    /// Desativa a entidade (soft delete), marcando-a como inativa.
    /// </summary>
    public void Deactivate()
    {
        if (!IsActive) return; 
        IsActive = false;
        // AddDomainEvent(new EntityDeactivatedEvent(...)) //events
    }

    /// <summary>
    /// Ativa a entidade, marcando-a como ativa/visível novamente.
    /// </summary>
    public void Activate() => IsActive = true;
}


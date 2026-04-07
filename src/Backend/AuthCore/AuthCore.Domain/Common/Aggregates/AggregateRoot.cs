using AuthCore.Domain.Common.Entities;

namespace AuthCore.Domain.Common.Aggregates;

/// <summary>
/// Representa o tipo base para raízes de agregado.
/// </summary>
public abstract class AggregateRoot : EntityBase
{
    #region Constructors

    /// <summary>
    /// Operação para criar instância da classe.
    /// </summary>
    protected AggregateRoot()
    {
    }

    /// <summary>
    /// Operação para criar instância da classe.
    /// </summary>
    /// <param name="id">Identificador único da entidade.</param>
    protected AggregateRoot(Guid id) : base(id)
    {
    }

    #endregion
}

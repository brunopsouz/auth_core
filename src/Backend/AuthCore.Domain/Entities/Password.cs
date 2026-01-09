using AuthCore.Domain.Common.Enums;

namespace AuthCore.Domain.Entities;

/// <summary>
/// 
/// </summary>
public sealed class Password
{
    /// <summary>
    /// 
    /// </summary>
    public Guid UserId { get; private set; }
    
    /// <summary>
    /// 
    /// </summary>
    public string Value { get; private set; } = null!;

    /// <summary>
    /// 
    /// </summary>
    public string SecretKey { get; private set; } = null!;

    /// <summary>
    /// 
    /// </summary>
    public int Attempts { get; private set; }

    /// <summary>
    /// 
    /// </summary>
    public PasswordStatus Status { get; private set; }

}
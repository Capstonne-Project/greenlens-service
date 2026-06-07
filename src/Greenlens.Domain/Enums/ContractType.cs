namespace Greenlens.Domain.Enums;

/// <summary>
/// Loại hợp đồng của Công ty Dịch vụ Môi trường.
/// </summary>
public enum ContractType
{
    /// <summary>Công ty trực thuộc (thuộc sở hữu/quản lý trực tiếp của Sở TNMT).</summary>
    Subsidiary,

    /// <summary>Công ty đấu thầu (ký hợp đồng thông qua đấu thầu công khai).</summary>
    Bidding
}

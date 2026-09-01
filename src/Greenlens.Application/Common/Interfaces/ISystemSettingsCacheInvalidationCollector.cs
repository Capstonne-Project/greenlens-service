namespace Greenlens.Application.Common.Interfaces;

/// <summary>
/// Trì hoãn refresh cache settings đến sau khi transaction commit.
/// </summary>
/// <remarks>
/// Handler PATCH gọi <see cref="Schedule"/> thay vì <see cref="ISystemSettingsCacheInvalidator.InvalidateAsync"/>
/// trực tiếp — nếu refresh trong transaction, DbContext mới đọc DB chưa commit → cache vẫn giá trị cũ.
/// </remarks>
public interface ISystemSettingsCacheInvalidationCollector
{
    void Schedule();

    bool TryConsumeScheduled();

    void Clear();
}

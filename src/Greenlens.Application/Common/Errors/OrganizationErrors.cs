using Greenlens.Domain.Common;

namespace Greenlens.Application.Common;

public static partial class Errors
{
    public static class Organization
    {
        public static Error DepartmentNotFound => new(
            "DEPARTMENT_NOT_FOUND",
            "Không tìm thấy đơn vị quản lý cấp tỉnh/thành phố.",
            ErrorType.NotFound);

        public static Error DepartmentAlreadyExists => new(
            "DEPARTMENT_ALREADY_EXISTS",
            "Tỉnh/thành phố này đã có đơn vị quản lý.",
            ErrorType.Conflict);

        public static Error OfficeNotFound => new(
            "OFFICE_NOT_FOUND",
            "Không tìm thấy văn phòng môi trường.",
            ErrorType.NotFound);

        public static Error MemberNotFound => new(
            "MEMBER_NOT_FOUND",
            "Không tìm thấy thành viên trong đội.",
            ErrorType.NotFound);

        public static Error LocalOfficeNotFound => new(
            "LOCAL_OFFICE_NOT_FOUND",
            "Không tìm thấy văn phòng cấp xã/phường.",
            ErrorType.NotFound);

        public static Error LocalOfficeAlreadyExists => new(
            "LOCAL_OFFICE_ALREADY_EXISTS",
            "Xã/phường này đã có văn phòng môi trường.",
            ErrorType.Conflict);

        public static Error TeamNotFound => new(
            "TEAM_NOT_FOUND",
            "Không tìm thấy đội môi trường.",
            ErrorType.NotFound);

        public static Error MemberAlreadyInTeam => new(
            "MEMBER_ALREADY_IN_TEAM",
            "Người dùng đã là thành viên của đội này.",
            ErrorType.Conflict);

        public static Error MemberNotInTeam => new(
            "MEMBER_NOT_IN_TEAM",
            "Người dùng không phải thành viên của đội này.",
            ErrorType.NotFound);

        public static Error InvalidRoleForOfficer => new(
            "INVALID_ROLE_FOR_OFFICER",
            "Người dùng phải có vai trò LEO để được gán cho văn phòng.",
            ErrorType.BusinessRule);

        public static Error InvalidRoleForDeo => new(
            "INVALID_ROLE_FOR_DEO",
            "Người dùng phải có vai trò DEO để được gán cho Sở TNMT.",
            ErrorType.BusinessRule);

        public static Error InvalidRoleForTeamMember => new(
            "INVALID_ROLE_FOR_TEAM_MEMBER",
            "Người dùng phải có vai trò Cleaner hoặc Inspector để tham gia đội.",
            ErrorType.BusinessRule);

        public static Error WardNotFound => new(
            "WARD_NOT_FOUND",
            "Mã xã/phường không tồn tại.",
            ErrorType.NotFound);

        public static Error ProvinceNotFound => new(
            "PROVINCE_NOT_FOUND",
            "Mã tỉnh/thành phố không tồn tại.",
            ErrorType.NotFound);

        public static Error OfficeNotOnboarded => new(
            "OFFICE_NOT_ONBOARDED",
            "Văn phòng xã/phường chưa được kích hoạt. Không thể điều phối task.",
            ErrorType.BusinessRule);

        public static Error InvalidRoleForRecruit => new(
            "INVALID_ROLE_FOR_RECRUIT",
            "Chỉ có thể recruit người dùng có vai trò Citizen.",
            ErrorType.BusinessRule);

        public static Error UserAlreadyInOffice => new(
            "USER_ALREADY_IN_OFFICE",
            "Người dùng đã thuộc một phường/xã khác.",
            ErrorType.Conflict);

        public static Error UserAlreadyInTeam => new(
            "USER_ALREADY_IN_TEAM",
            "Người dùng đã là thành viên của một đội khác.",
            ErrorType.Conflict);

        public static Error TeamNotInOffice => new(
            "TEAM_NOT_IN_OFFICE",
            "Đội không thuộc văn phòng của bạn.",
            ErrorType.BusinessRule);

        public static Error OfficerNoOffice => new(
            "OFFICER_NO_OFFICE",
            "Bạn chưa được gán cho văn phòng nào.",
            ErrorType.BusinessRule);

        public static Error TransferSameTeam => new(
            "TRANSFER_SAME_TEAM",
            "Không thể chuyển thành viên sang chính đội hiện tại.",
            ErrorType.BusinessRule);

        public static Error CompanyNotFound => new(
            "COMPANY_NOT_FOUND",
            "Không tìm thấy công ty dịch vụ môi trường.",
            ErrorType.NotFound);

        public static Error NotCompanyManager => new(
            "NOT_COMPANY_MANAGER",
            "Bạn không phải CompanyManager hoặc chưa được gán cho công ty nào.",
            ErrorType.Forbidden);

        public static Error TeamNotInCompany => new(
            "TEAM_NOT_IN_COMPANY",
            "Đội không thuộc công ty của bạn.",
            ErrorType.BusinessRule);

        public static Error CompanyContractNumberExists => new(
            "COMPANY_CONTRACT_NUMBER_EXISTS",
            "Số hợp đồng đã tồn tại trong hệ thống.",
            ErrorType.Conflict);

        public static Error ManagerEmailAlreadyExists => new(
            "MANAGER_EMAIL_ALREADY_EXISTS",
            "Email này đã được sử dụng cho tài khoản khác.",
            ErrorType.Conflict);

        public static Error StaffNotFound => new(
            "STAFF_NOT_FOUND",
            "Không tìm thấy nhân viên trong công ty.",
            ErrorType.NotFound);

        public static Error StaffNotInCompany => new(
            "STAFF_NOT_IN_COMPANY",
            "Nhân viên không thuộc công ty của bạn.",
            ErrorType.BusinessRule);

        public static Error TeamAlreadyDeactivated => new(
            "TEAM_ALREADY_DEACTIVATED",
            "Đội đã bị vô hiệu hóa trước đó.",
            ErrorType.BusinessRule);

        // ── Invitation (BR-ORG-021) ──

        public static Error InvitationNotFound => new(
            "INVITATION_NOT_FOUND",
            "Không tìm thấy lời mời.",
            ErrorType.NotFound);

        public static Error InvitationExpired => new(
            "INVITATION_EXPIRED",
            "Lời mời đã hết hạn hoặc đã được sử dụng.",
            ErrorType.BusinessRule);

        public static Error InvitationAlreadyResponded => new(
            "INVITATION_ALREADY_RESPONDED",
            "Lời mời đã được trả lời trước đó.",
            ErrorType.BusinessRule);

        public static Error DuplicateInvitation => new(
            "DUPLICATE_INVITATION",
            "Người dùng này đã có lời mời đang chờ xử lý.",
            ErrorType.Conflict);

        // ── Release Staff ──

        public static Error CannotReleaseCitizen => new(
            "CANNOT_RELEASE_CITIZEN",
            "Người dùng đã là Citizen, không cần release.",
            ErrorType.BusinessRule);

        public static Error UserNotInYourOffice => new(
            "USER_NOT_IN_YOUR_OFFICE",
            "Người dùng không thuộc phường/xã bạn quản lý.",
            ErrorType.Forbidden);

        // ── Company Status (BR-CMP-004) ──

        public static Error CompanyNotActive => new(
            "COMPANY_NOT_ACTIVE",
            "Công ty không ở trạng thái Active, không thể tạm ngưng.",
            ErrorType.BusinessRule);

        public static Error CompanyCannotTerminate => new(
            "COMPANY_CANNOT_TERMINATE",
            "Không thể chấm dứt công ty từ trạng thái hiện tại.",
            ErrorType.BusinessRule);

        public static Error CompanyNotSuspended => new(
            "COMPANY_NOT_SUSPENDED",
            "Chỉ có thể kích hoạt lại công ty đang bị tạm ngưng.",
            ErrorType.BusinessRule);

        public static Error CompanyAlreadyDeleted => new(
            "COMPANY_ALREADY_DELETED",
            "Công ty đã được xóa trước đó.",
            ErrorType.Conflict);

        public static Error CompanyMustTerminateFirst => new(
            "COMPANY_MUST_TERMINATE_FIRST",
            "Phải chấm dứt hợp đồng (Terminate) trước khi xóa công ty.",
            ErrorType.BusinessRule);

        public static Error TeamAlreadyDeleted => new(
            "TEAM_ALREADY_DELETED",
            "Team đã được xóa trước đó.",
            ErrorType.Conflict);

        public static Error TeamHasActiveAssignments => new(
            "TEAM_HAS_ACTIVE_ASSIGNMENTS",
            "Không thể xóa team đang có nhiệm vụ đang xử lý.",
            ErrorType.BusinessRule);

        // ── Contract Renewal (BR-CMP-006) ──

        public static Error SubsidiaryCannotRenew => new(
            "SUBSIDIARY_CANNOT_RENEW",
            "Công ty trực thuộc (Subsidiary) có hợp đồng vô thời hạn, không thể gia hạn.",
            ErrorType.BusinessRule);

        public static Error ContractNumberAlreadyUsed => new(
            "CONTRACT_NUMBER_ALREADY_USED",
            "Số hợp đồng mới đã tồn tại trong hệ thống.",
            ErrorType.Conflict);

        // ── Data Isolation (BR-CMP-021) ──

        public static Error CrossCompanyAccess => new(
            "CROSS_COMPANY_ACCESS",
            "Bạn không có quyền truy cập dữ liệu của công ty khác.",
            ErrorType.Forbidden);

        /// <summary>BR-CMP-020: DEO/Admin phải truyền CompanyId khi xem KPI.</summary>
        public static Error CompanyIdRequired => new(
            "COMPANY_ID_REQUIRED",
            "CompanyId là bắt buộc cho DEO/Admin.",
            ErrorType.Validation);
    }
}

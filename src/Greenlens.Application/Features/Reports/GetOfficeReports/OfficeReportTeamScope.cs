namespace Greenlens.Application.Features.Reports.GetOfficeReports;

/// <summary>Filter LEO office report list by who handles the active assignment cycle.</summary>
public enum OfficeReportTeamScope
{
    All,
    /// <summary>Report dispatched to an environmental service company and/or assigned to a company team.</summary>
    Company,
    /// <summary>Report handled by a community (ward) team — not company-dispatched.</summary>
    Community
}

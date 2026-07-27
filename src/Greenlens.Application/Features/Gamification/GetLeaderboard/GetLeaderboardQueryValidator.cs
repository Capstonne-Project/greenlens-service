using FluentValidation;
using Greenlens.Application.Features.Gamification;
using Greenlens.Domain.Enums;

namespace Greenlens.Application.Features.Gamification.GetLeaderboard;

public sealed class GetLeaderboardQueryValidator : AbstractValidator<GetLeaderboardQuery>
{
    public GetLeaderboardQueryValidator()
    {
        RuleFor(x => x.Top)
            .InclusiveBetween(1, 100);

        RuleFor(x => x.Month)
            .InclusiveBetween(1, 12)
            .When(x => x.Month.HasValue);

        RuleFor(x => x.Year)
            .InclusiveBetween(GamificationHelpers.MinLeaderboardYear, 2100)
            .When(x => x.Year.HasValue);

        RuleFor(x => x.Month)
            .Null()
            .When(x => x.Period != LeaderboardPeriod.Monthly)
            .WithMessage("month is only supported when period is Monthly.");

        RuleFor(x => x.Year)
            .Null()
            .When(x => x.Period is LeaderboardPeriod.AllTime or LeaderboardPeriod.Weekly)
            .WithMessage("year is only supported when period is Monthly or Yearly.");
    }
}

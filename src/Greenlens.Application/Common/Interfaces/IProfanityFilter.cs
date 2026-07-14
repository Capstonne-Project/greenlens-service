namespace Greenlens.Application.Common.Interfaces;

/// <summary>BR-CMT-003: word filter for comment content (phase 1 — no AI text endpoint).</summary>
public interface IProfanityFilter
{
    bool ContainsProfanity(string text);
}

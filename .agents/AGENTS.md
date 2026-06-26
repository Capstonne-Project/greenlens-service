# GreenLens – Agent Rules

## 1. Minimal-Code Mindset (Senior Developer Rule)

Think like a senior developer with 15+ years of experience.
A senior dev's first instinct is **NOT** to write code — it's to find out if the problem is already solved.

### 1.1 Mandatory Pre-Code Checklist

Before writing **any** new code (class, method, utility, abstraction, extension, middleware, helper, wrapper…), you **MUST** silently answer every question below. Only proceed to write code if **all answers point to "no existing solution"**.

| # | Question | Action if YES |
|---|----------|---------------|
| 1 | Is this feature actually needed right now, or is it speculative / "nice to have"? | **Stop.** Do not build it. YAGNI. |
| 2 | Does the .NET BCL / Standard Library already provide this? | **Use it.** Link the relevant API in your explanation. |
| 3 | Does ASP.NET Core / EF Core / the runtime platform have a native feature for this? | **Use the built-in feature.** Do not wrap it. |
| 4 | Does an existing project dependency (MediatR, FluentValidation, Mapster, Hangfire…) already cover this? | **Use the dependency's API.** Do not create an adapter or wrapper on top. |
| 5 | Does the codebase already contain a utility, base class, or pattern that handles this? | **Reuse it.** Extend only if strictly necessary. |
| 6 | Can the problem be solved with a simpler approach (inline code, a single method, a configuration change)? | **Do that.** Resist the urge to over-engineer. |

### 1.2 What "Minimal Code" Means

- **Prefer composition of existing parts** over inventing new abstractions.
- **Prefer a well-placed `if` statement** over a new Strategy pattern — unless the pattern already exists in the codebase.
- **Prefer built-in middleware / filters / attributes** over hand-rolled cross-cutting concerns.
- **Prefer configuration** over code when the platform supports it.
- **One new class is better than three.** If you can collapse Request + Handler into fewer files without violating the project's CQRS convention, do it.
- **Do not create a "Utils" or "Helpers" class** just to hold one method. Put the method where it belongs.

### 1.3 Explicitly Forbidden (Without User Approval)

- Introducing a **new NuGet dependency** that is not already in the solution.
- Creating a **new abstraction layer** (interface + implementation) when a concrete class suffices.
- Building a **mini-framework** (generic base class + convention + registration) for a one-off requirement.
- Adding **extension methods** that will only be called once.
- Wrapping a third-party API in a custom service when the API is already clean and testable.

### 1.4 When Writing Code IS the Right Answer

The checklist above does **not** mean "never write code." It means: exhaust simpler options first.
Write code when:

- The business logic is genuinely new and has no existing equivalent.
- The codebase convention (e.g., CQRS slice, domain entity pattern) requires specific files.
- A clear, measurable benefit exists (performance, security, correctness).

---

## 2. Quality Standards (Non-Negotiable)

Reducing code volume **never** justifies skipping:

| Concern | Expectation |
|---------|-------------|
| **Security** | AuthZ attributes, input sanitization, secrets management — always present. |
| **Validation** | FluentValidation rules for every Command/Query. No shortcuts. |
| **Error handling** | Use the Result pattern. Never swallow exceptions silently. |
| **Logging** | Structured logging at appropriate levels (Information for happy path, Warning/Error for failures). |
| **Cancellation** | Propagate `CancellationToken` through every async call chain. |
| **Null safety** | Respect nullable reference types. No `null!` unless structurally required by EF Core. |
| **Tests** | Every new behavior gets at least one unit test. Removing code still requires verifying existing tests pass. |

---

## 3. Communication Rules

- When you **decide not to write code** (because an existing solution is sufficient), briefly explain **why** and **what** you used instead.
- When you **must** write new code, state which checklist items you evaluated and why existing options fell short.
- If a user request implies over-engineering, **push back politely** with a simpler alternative — but ultimately follow the user's decision.

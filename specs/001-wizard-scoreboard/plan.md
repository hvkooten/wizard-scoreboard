# Implementation Plan: Wizard Scoreboard App

**Branch**: `001-wizard-scoreboard` | **Date**: 2026-04-02 | **Spec**: specs/001-wizard-scoreboard/spec.md
**Input**: Feature specification from `specs/001-wizard-scoreboard/spec.md`

## Summary

Cross-platform .NET MAUI scorekeeping app for Wizard card game with strong emphasis op performance (dashboard <2s), scanbare UI, en clean moderne styling. 

Belangrijkste modules:
- Instellingen en groepenbeheer (3–6 spelers)
- Highscore & ranking
- Scoreblok met rondebeheer, delerlogica, troefkleur, voorspellingen
- Spelregels pagina met embedded PDF of text samenvatting
- UI in C# code (geen XAML), unit tests met >=80% coverage

## Technical Context

**Language/Version**: .NET 10+ (of nieuwste stable op moment van ontwikkeling) MAUI
**Primary Dependencies**: .NET MAUI, CommunityToolkit.Maui, SQLite-net-pcl (of EntityFramework Core Sqlite), AppCenter en/of Sentry facultatief
**Storage**: lokale SQLite via MAUI DataStore / Preferences + mogelijk in-app JSON fallback
**Testing**: NUnit/xUnit + MAUI UITest (of .NET MAUI CommunityToolkit.Maui.Testing), cobertura-achtige dekking
**Target Platform**: iOS (iPhone), Android phones, Windows desktop
**Project Type**: mobiele/desktop app - cross-platform UI
**Performance Goals**: dashboard load <2s in productie
**Constraints**: minimale abstrahering, weinig architectuur-overhead, geen auteursrechtafbeeldingen
**Scale/Scope**: MVP alleen lokale scores (geen netwerk), 3-6 spelers groepen, max 20 rondes

## Constitution Check

- [x] Latest .NET MAUI + SDK doordrukken (constitution-vereiste)
- [x] TDD met unit tests + 80% coverage (constitution-vereiste)
- [x] UI in C# code (geen XAML) (constitution-vereiste)
- [x] Eenvoudig architectuurontwerp, YAGNI

## Project Structure

### Documentation (this feature)

```text
specs/001-wizard-scoreboard/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
└── tasks.md
```

### Source Code

- `src/WizardScoreboard/` (project)
- `src/WizardScoreboard.Tests/` (unit tests)
- `src/WizardScoreboard.Ui/` (optioneel shared UI component library)
- `src/WizardScoreboard.Data/` (data access abstractions)

## Phase 0: Outline & Research

### Unknowns (none blijven, alle vereisten inzichtelijk)
- Implementatie van “show picture during loading” moet rechtenvrij: gebruik inline vector/DRM-safe icon in code, geen externe auteursrechtbeelden.
- Troefkleurachtergrond en dynamische kleuraanpassing (C# code UI) vereist platform-compatibele kleurbeheer.

### Research acties
- Onderzoek beste praktijk voor cross-platform lokale data (SQLite + dependency injection in MAUI).
- Normen voor invoervalidatie (0..rondenom) + rondeprikkel logica.
- Snelheidsmeting dashboard <2 sec met `AppShell` en pre-loading van gegevens in background.
- Hoe inapp-browser of embedded PDF-viewer doen in MAUI (iOS/Android/Win).

### research.md (output)

- Decision: `SQLite` met `Repository`-interface, in MAUI met `DbContext` + local file.
- Rationale: nieuwste, stabiel, inspecteerbaar, offline/compact.
- Alternatives: enkel Preferences/JSON (te beperkt voor relatie-data).

## Phase 1: Design & Contracts

### data-model.md

Entiteiten:
- Group: id, name, createdAt, players
- Player: id, groupId, name, roundOrder, wins, bestScore
- ScoreSession: id, groupId, startDate, currentRound, maxRounds, trumpSuit
- RoundEntry: sessionId, roundIndex, dealerId, bidPerPlayer, resultPerPlayer, trumpSuit
- HighscoreEntry: playerId, totalWins, highestScore

Validatie:
- `playerCount` 3..6
- `maxRounds` per playerCount
- `bid` 0..currentRound
- troef kleuren set enum: Hearts, Diamonds, Clubs, Spades, None

### contracts/

`contracts/api.md` (optioneel intern contract) voor Model API:
- `IGroupService` (CreateGroup, UpdateGroup, DeleteGroup, GetGroups)
- `IScoreService` (StartSession, RecordRound, NextRound, GetSession)
- `IHighscoreService` (GetTopHighscores, UpdateHighscores)

### quickstart.md

Toegankelijk stappen:
1. Clone repository
2. `dotnet workload install maui`
3. `dotnet restore`
4. `dotnet test --configuration Release` (coverage >80% instellen met `coverlet`)
5. `dotnet build -t:Run -f net8.0-android` / `net8.0-ios` / `net8.0-windows`

### update-agent-context

Volgen: `.specify/scripts/powershell/update-agent-context.ps1 -AgentType copilot`.

## Phase 2: Implementation Tasks

### tasks.md (high-level)

- [US1,P1] Setup MAUI Shell + trajecttabs: Settings, Rules, Highscore, Scoreblok
- [US1,P1] Implement Group management UI + data layer
- [US1,P1] Implement player CRUD binnen groep + order via drag/drop
- [US2,P1] Implement Score Session engine + ronde/deler/troef logica
- [US2,P1] Implement ronde start popup + input validation (0..round)
- [US2,P1] Dynamische troefkleur UI achtergronden
- [US3,P2] Implement Rule page with PDF viewer + fallback summary text
- [US3,P2] Implement Highscore page sorted by wins
- [all] Testing: unit tests voor services, business logic, UI componenten
- [all] Performance: dashboard load path pre-fetch user group list + status

---

## Gates

- TECHNICAL: Geen [NEEDS CLARIFICATION] markeringen.
- GOVERNANCE: Voldoet aan constitutie-check.

## Output

Bouwbestand: `specs/001-wizard-scoreboard/plan.md`.
Branch-check: lokale branch nog niet aangemaakt, kan via `git checkout -b 001-wizard-scoreboard`.

---

## Extension Hooks

Geen `.specify/extensions.yml` aanwezig; geen pre/post hooks gedetecteerd.

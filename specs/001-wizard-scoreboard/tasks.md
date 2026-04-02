---
description: "Task list for Wizard Scoreboard App implementation"
---

# Tasks: Wizard Scoreboard App

**Feature**: specs/001-wizard-scoreboard/spec.md

## TASK-001: Create Wizard Score Board App

**Description**: Implement Wizard Score Board App for iPhone, Android en Windows.

**Dependencies**: TASK-001 (Project setup)

**Acceptance Criteria**:
- [ ] app created in `src/scoreboard`
- [ ] multiple languages implemented
- [ ] start and end a game
- [ ] handles errors gracefully

**Complexity**: Medium

**Estimated Time**: 1 hour

### Subtasks

1. [P1] Initial MAUI project scaffold
   - `dotnet new maui -n WizardScoreboard -o src/scoreboard`
   - Verifieer targets: `net8.0-android`, `net8.0-ios`, `net8.0-windows`

2. [P1] Configure localization
   - Voeg resourcebestanden toe voor `nl-NL`, `en-US`, `de-DE`, `es-ES`, `fr-FR`
   - Implementeer een eenvoudige language switch in instellingen (via `CultureInfo.DefaultThreadCurrentCulture` / `AppResources`)

3. [P1] Basis navigatie (Shell)
   - Instellingen, Spelregels, Highscore, Scoreblok beschikbaar
   - Startpagina laadt <2s via eenvoudige layout + dataload minimalisatie

4. [P2] Score session engine (game start/stop)
   - `GameSessionService` met `StartGame(groupId)`, `EndGame()`, `NextRound()`
   - Validaties: spelercount 3..6, rondecount volgens tabel (20/15/12/10)

5. [P2] UI scoreblok (C# code)
   - Ronde-indicator, deler status, voorspelling popup, troefkleur achtergrond
   - Pop-up voor verwachte slagen: tekstinvoer (+18), dropdown troef + kleur.

6. [P2] Error handling
   - Gebruik `try/catch` in services + UI message display (`DisplayAlert`)
   - Inputfouten validatie: `0 <= voorspelling <= rondeAantal`
   - Geen crash; fallback naar laatste valide status

7. [P2] Unit tests
   - Project `src/scoreboard/tests` of `src/WizardScoreboard.Tests`
   - Test game start/stop, round logic, language switch, error input.
   - Coverage met `coverlet` check `>80%`

8. [P3] Optional: embedded spelregels (PDF)
   - Gebruik MAUI `WebView` to load asset/pdf URL
   - Fallback text als PDF niet beschikbaar

9. [P3] Highscore lijst
   - Eenvoudige `IHighscoreService`  om total wins te tonen
   - UI in C# - gesorteerd desc.


## Notes

- Houd architectuur eenvoudig: service-klassen + viewmodel per pagina + minimal DI
- “show picture during loading”: use lokale vector/FontIcon of `ActivityIndicator` (geen auteursrecht afbeelding)
- Volg constitutie: geen XAML UI-structuur, maar kan in code opgebouwd worden (of minimal XAML als boilerplate, met expliciete switch naar C# viewcomponenten)

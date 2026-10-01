# Feature Specification: Wizard Scoreboard App

**Feature Branch**: `001-wizard-scoreboard`  
**Created**: 2026-04-02  
**Status**: Draft  
**Input**: User description: "Ik wil een applicatie in .NET MAUI voor het spel wizard. CRITICAL EXPERIENCES: - Dashboard should load quickly (under 2 seconds) - Information should be scannable at a glance - Visual design should be clean and modern - De applicatie moet op een iPhone en een Android phone te draaien zijn en een versie die op Windows draait. - Er mogen geen plaatjes in zitten met auteurs rechten. Het is een score kaart voor het kaart spel. 1. Er moeten settings zijn a. Daar moet je de taal kunnen selecteren, met o.a. Nederlands, Engels, Duits, Spaans en Frans. b. Groepen kunnen bekijken, aanpassen of aanmaken, met 3 tot 6 spelers. i. Per speler moet de naam, aantal gewonnen potjes en hoogste score bijgehouden worden. ii. In een groep moet de volgorde aangepast moeten kunnen worden 2. Er moet een pagina zijn met spelregels a. Deze kan je hier vinden: https://cdn.1j1ju.com/medias/f1/8e/ad-wizard-rulebook.pdf 3. HighScore lijst met aantal gewonnen potjes. 4. Het scoreblok. Dit is de belangrijkste pagina Deze moet eruit zien zoals het bijgevoegde plaatje Voor 3 spelers er zijn 20 potjes Voor 4 spelers er zijn 15 potjes Voor 5 spelers er zijn 12 potjes Voor 6 spelers er zijn 10 potjes Laat per ronde zien wie er moet delen. Laat na de start een popup zien met alle spelers en een invoer veld voor verwachte gewonnen slagen (tussen 0 en nummer van het potje) en welke kleur de troef is en geef de achtergrond deze kleur."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Spelgroepbeheer en instellingen (Priority: P1)

Als een speler wil ik groepen kunnen maken, openen, aanpassen en verwijderen, zodat ik meerdere Wizard-communities kan beheren.

**Waarom deze prioriteit**: basisfunctionaliteit voor iedere gebruiker, vereist voordat scoreblok kan werken.

**Independent Test**: navigeer naar instellingen → groepen; maak een nieuwe groep met 4 spelers; pas spelernaam en volgorde aan; controleer opslag en herstel.

**Acceptance Scenarios**:

1. **Given** gebruiker opent instellingen, **When** gebruiker kiest "Groepen", **Then** ziet de gebruiker een lijst met bestaande groepen en een knop "Nieuwe groep".
2. **Given** een nieuwe groep is aangemaakt met 4 spelers, **When** gebruiker slaat op, **Then** wordt de groep opgeslagen met naam, spelers en default scorewaarden (0).
3. **Given** een groep bestaat, **When** gebruiker wijzigt spelernaam of volgorde, **Then** worden wijzigingen opgeslagen en effectueel voor scoreblok.

---

### User Story 2 - Scoreblok wedstrijdregistratie (Priority: P1)

Als een speler wil ik een scoreblok gebruiken dat ronde-aantallen, deler en troefkleur toont, zodat ik het Wizard-spel correct kan bijhouden.

**Waarom deze prioriteit**: kernfunctionaliteit van de app; direct waarde voor speler.

**Independent Test**: start een scoreblok voor 4 spelers; voer per ronde voorspelde slagen in; controleer roldelen en opslag.

**Acceptance Scenarios**:

1. **Given** een groep met 4 spelers actief is, **When** gebruiker start scoreblok, **Then** opent scoreblok met 15 rondes, dele-er wordt voor ronde 1 getoond.
2. **Given** ronde start, **When** gebruiker voert voorspelde slagen (tussen 0 en ronde nummer) en kiest troefkleur, **Then** toont app een popup met invoer en past achtergrondkleur aan volgens troef.
3. **Given** gebruiker voltooit een ronde, **When** volgende ronde begint, **Then** rolt de deler door en ronde teller verhoogt.

---

### User Story 3 - Spelregels en highscore (Priority: P2)

Als een speler wil ik de Wizard spelregels kunnen lezen en highscorelijst zien, zodat ik makkelijk kan nalezen en competitie bijhouden.

**Waarom deze prioriteit**: verbetert gebruikerservaring en kennis van spelregels.

**Independent Test**: open spelregelspagina; controleer pdf-link of samenvatting. Open highscorepagina; verifieer gesorteerde lijst op gewonnen potjes.

**Acceptance Scenarios**:

1. **Given** gebruiker navigeert naar "Spelregels", **When** wordt pagina geopend, **Then** toont de inhoud uit het aangeleverde pdf-document in tekst of PDF-viewer.
2. **Given** gespeelde groepen met scores, **When** navigeert naar "Highscore", **Then** toont lijst gesorteerd op aantal gewonnen potjes en laat top 10 zien.

## Functional Requirements *(mandatory)*

1. Dashboard laadt binnen 2 seconden op iPhone, Android en Windows.
2. UI toont duidelijke segments: Instellingen, Spelregels, Highscore, Scoreblok.
3. Instellingen:
   - Taalkeuze: Nederlands, Engels, Duits, Spaans, Frans.
   - Groepen: CRUD voor groepen met 3-6 spelers.
   - Spelers: naam, aantal gewonnen potjes, hoogste score.
   - Volgorde spelers wijzigen.
4. Spelregelspagina: link naar pdf en uitgelichte secties.
5. Highscorelijst: ranking op gewonnen potjes.
6. Scoreblok:
   - 3 spelers: 20 rondes, 4 spelers: 15, 5 spelers: 12, 6 spelers: 10.
   - Rondeweergave, delerindicator.
   - Start-popup met alle spelers, invoer voor voorspelde slagen (0..ronde) en troefkleur.
   - Achtergrond verandert in troefkleur na selectie.
7. Geen auteursrechtelijk beheerde afbeeldingen; gebruik vector/privéontwerpen of standaardelementen.
8. App moet op iOS/Android/Windows kunnen draaien (MAUI cross-platform).

## Success Criteria *(mandatory)*

- 95% van gebruikers kan binnen 2 sec bij dashboard komen in test (door load metingen).
- 90% van testgebruikers vindt informatie overzichtelijk (usability score > 4/5).
- Spelgroepbeheer werkt zonder fouten voor 3-6 spelers in 100% van testgevallen.
- Scoreblok rondes matchen spelercount/roundrules in 100% van tests.
- Troefkleurselectie verandert achtergrondkleur in 100% van scenario’s en valide input (0..ronde).
- Highscore toont correcte rangschikking op gewonnen potjes.

## Key Entities *(mandatory where data involved)*

- Groep
  - id, naam, spelers, datumAangemaakt
- Speler
  - id, naam, gewonnenPotjes, hoogsteScore, volgorde
- ScoreBlock
  - groepId, spelers, huidigeRonde, maxRondes, deklarerendeDeler, troefKleur
- ScoreRonde
  - rondeNummer, voorspeldeSlagen[], werkelijkeSlagen[], troefKleur
- HighscoreEntry
  - spelerId, naam, gewonnenPotjes, hoogsteScore

## Assumptions

- Data wordt lokaal bewaard (ongeveer app-settings / lokale database), geen remote backend verplicht voor MVP.
- Troefkleuren zijn minimaal 4 voorgedefinieerde kleuren (harten, ruiten, klaveren, schoppen).
- Spelregels kunnen als ingesloten tekst of via embedded pdf-link worden aangeboden.
- UI wordt geimplementeerd in C# code volgens constitutie (geen XAML).

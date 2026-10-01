# Data Model: Wizard Scoreboard

## Entities

- Group
  - id (GUID)
  - name (string)
  - createdAt (DateTime)
  - players (List<Player>)

- Player
  - id (GUID)
  - name (string)
  - order (int)
  - wins (int)
  - highestScore (int)
  - currentPoints (int)

- ScoreSession
  - id (GUID)
  - groupId (GUID)
  - startDate (DateTime)
  - currentRound (int)
  - maxRounds (int)
  - trump (TrumpSuit enum)
  - isActive (bool)
  - rounds (List<RoundEntry>)

- RoundEntry
  - roundNumber (int)
  - dealerPlayerId (GUID)
  - bidByPlayer (Dictionary<Guid,int>)
  - actualByPlayer (Dictionary<Guid,int>)
  - trump (TrumpSuit enum)

- HighscoreEntry
  - playerId (GUID)
  - name (string)
  - totalWins (int)
  - highestScore (int)

## Relationships

- 1 Group has 3..6 Players.
- 1 Group can have 0..* ScoreSessions.
- 1 ScoreSession has 1..maxRounds RoundEntries.
- Each RoundEntry is associated with all players of the session.

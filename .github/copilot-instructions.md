# Copilot Instructions

These instructions apply only to the WizardScoreboard solution in this repository.

## Testing
- Wait with running unit test, till all unit tests are created and the solution builds successfully, to avoid unnecessary test failures.

## Versioning
- Increment `<ApplicationVersion>` in `src/scoreboard/WizardScoreboard.csproj` by 1 on every user request that changes code, so the user can see there is a new build.

## Bug Reporting
- For WizardScoreboard bug reports, provide technical details in English in every app language, except for the technical-information heading, which should remain localized.
- Include the app version in the report subject for email and clipboard reports.

## User Interface Guidelines
- In WizardScoreboard's tricks-entry and latest-round editing dialogs, keep player names and their original bids visible, including the dealer designation; bids remain read-only when correcting results.

## Beta Welcome Message
- In WizardScoreboard's beta welcome message, remind users to take and attach screenshots of bugs/issues.

<!-- Sync Impact Report
Version change: none → 1.0.0
List of modified principles: N/A (new constitution)
Added sections: Core Principles (4), Technology Stack, Development Workflow, Governance
Removed sections: none
Templates requiring updates: ✅ updated plan-template.md, ✅ updated tasks-template.md, spec-template.md (no changes needed), commands/*.md (none exist)
Follow-up TODOs: none
-->

# Wizard Scoreboard Constitution

## Core Principles

### I. Test-Driven Development
All code must be developed using Test-Driven Development (TDD). Unit tests are mandatory for all code with a minimum coverage of 80%. Tests must be written before implementation and must pass before code is considered complete.

### II. C# UI Implementation
All user interface screens must be implemented using C# code. XAML is not permitted for UI definitions to maintain consistency and simplicity.

### III. Simple Architecture
Architectures must remain simple and straightforward. Avoid unnecessary abstractions, over-engineering, and complex design patterns. Follow YAGNI (You Aren't Gonna Need It) principles to keep the codebase maintainable.

### IV. Latest Versions
Always use the latest stable versions of .NET MAUI, .NET SDK, and all project dependencies to ensure security, performance, and access to new features.

## Technology Stack

The application is built using .NET MAUI for cross-platform mobile and desktop development. The primary programming language is C#. All development must adhere to the latest .NET standards and MAUI best practices.

## Development Workflow

All code changes require thorough code reviews. Unit tests must pass with at least 80% coverage before merging. User interfaces must be implemented in C# code only. Pull requests must demonstrate compliance with all core principles.

## Governance

This constitution supersedes all other development practices and guidelines. Amendments to the constitution require team consensus, documentation of changes, and a migration plan for existing code. All development activities must verify compliance with these principles.

**Version**: 1.0.0 | **Ratified**: 2026-04-02 | **Last Amended**: 2026-04-02

# smsflow-dotnet Agent Instructions

This repository is intended to become the public .NET SDK for SMSFlow.

- Do not commit real SMSFlow credentials, phone numbers, tokens, customer data, logs, or package signing secrets.
- Keep SDK code dependency-light and testable with fake HTTP handlers.
- Public APIs should be stable, documented, and friendly for ASP.NET Core dependency injection.
- Use `.env.example` only for local examples.

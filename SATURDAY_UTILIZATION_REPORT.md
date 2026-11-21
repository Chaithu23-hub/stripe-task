# Saturday Utilization Report

**Date** 21 November 2026
**Task** Angular, ASP.NET Core, SQL Server, and Stripe checkout

## Implementation summary

Extended the existing Angular project into a responsive storefront for laptops, mobile phones, and headphones. The storefront includes a searchable catalog, product details, a persistent cart, a validated guest checkout form, and order and payment status pages. Moved the frontend, backend, and tests into the `stripe-task` folder.

The .NET 10 solution follows Clean Architecture. The Domain project contains the store entities and payment rules. The Application project handles checkout and payment confirmation through interfaces. The Infrastructure project implements SQL Server and Stripe integrations. The API project exposes HTTP endpoints and manages browser-scoped guest access.

## Topics covered

- **Angular** — Standalone components, input and output bindings, directives, pipes, lifecycle hooks, lazy routes, functional guards, typed reactive forms, and HTTP interceptors.
- **State and asynchronous work** — Signals and computed values for cart totals, NgRx actions and effects for the catalog, RxJS for debounced search and order polling, and OnPush change detection.
- **.NET and SQL Server** — ASP.NET Core controllers, dependency injection, DTO validation, EF Core migrations, server-side INR pricing, and transactional updates.
- **Stripe** — Hosted Checkout, test-mode configuration, idempotent session creation, signature verification, duplicate-event handling, and backend-confirmed payment status.

## Challenges and resolutions

- Isolated the SQL Server and Stripe SDK dependencies in Infrastructure while preserving the original API routes and database migration history.
- Preserved the existing SQL Server row-version column as a concurrency token when moving the EF model.
- Added server-side validation and database uniqueness to prevent invalid carts and repeated checkout references from reaching Stripe as new sessions.

## Verification and remaining setup

- Angular production build and seven automated tests passed.
- The .NET solution built and the EF migrations were applied to SQL Server LocalDB.
- Eleven API tests passed and cover INR conversion, server-side totals, invalid requests, checkout retries, guest order isolation, and signed webhook processing with a stubbed Stripe endpoint.
- A mobile-size Microsoft Edge smoke test passed for catalog search and sorting, cart persistence, checkout validation, order navigation, and the cancellation page.
- Full Stripe sandbox verification requires a test secret key, Stripe CLI, and the listener signing secret. The setup guide includes the commands and manual test scenarios.

The local application demonstrates the full checkout design with a real SQL Server database and automated coverage of the Stripe integration boundary. Hosted payment and webhook delivery from the Stripe sandbox remain to be verified after credentials are configured.

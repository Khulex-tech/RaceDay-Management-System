# RaceDay API Endpoint Plan (Part 1, Section B)

**Module:** PROG6212 - Programming 2B
**System:** RaceDay - Event Management Platform for South African Road Running, Walking and Cycling Events
**Document purpose:** This is the specification that the ASP.NET Core Web API in Part 2 will be built against. Every endpoint below is planned before any API code is written.

---
## 1. Conventions

| Concern | Decision |
| --- | --- |
| Base URL | `https://localhost:7043/api` |
| Format | JSON request and response bodies (`application/json`), except file uploads which use `multipart/form-data` (Part 3). |
| Authentication | Server-side **session** authentication (`ISession`). On successful login the API stores `UserId` and `Role` in the session and returns the session cookie. All subsequent requests are authorised from the session, not from the client payload. |
| Passwords | Never stored or returned in plain text. Hashed with a per-user salt (PBKDF2) before persistence. |
| Role values | `Organiser`, `Participant`. |
| Route style | Plural nouns, lowercase, hierarchical for owned resources (e.g. `/api/events/{eventId}/categories`). Verbs only for non-CRUD actions (`/login`, `/logout`, `/enter`). |
| Ownership rule | An Organiser may only modify events they created, and only view enrolments/results belonging to their own events. A Participant may only view or modify their own profile, enrolments and results. Violations return **403 Forbidden**, not 404, so the client can distinguish "not yours" from "does not exist". |
| Standard failure codes | `400` validation error, `401` no active session, `403` wrong role or not the owner, `404` resource does not exist, `409` business-rule conflict (duplicate email, duplicate enrolment, event full). |
| Dates and times | ISO 8601 in UTC (`2026-11-01T06:00:00Z`). Finish times use `hh:mm:ss`. |

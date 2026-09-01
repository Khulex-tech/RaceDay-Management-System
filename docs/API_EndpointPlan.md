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
### Role column key

- **None** — public, no session required.
- **Any** — any authenticated user (Organiser or Participant).
- **Organiser** / **Participant** — that role only.

---

## 2. Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/auth/register` | Creates a new user account and assigns the role chosen at sign-up (Organiser or Participant). Hashes the password before saving. Exists so that both user types self-register through one endpoint. | None | `{ "firstName", "lastName", "email", "password", "confirmPassword", "phoneNumber", "dateOfBirth", "city", "role" }` | `201 Created` — `{ userId, firstName, lastName, email, role }`. `400 Bad Request` — validation failed (weak password, passwords do not match, invalid role, participant under 12). `409 Conflict` — email already registered. |
| POST | `/api/auth/login` | Authenticates a user against the stored password hash and creates the server-side session holding `UserId` and `Role`. Exists as the single entry point to an authenticated state. | None | `{ "email", "password" }` | `200 OK` — `{ userId, fullName, role }` plus session cookie. `400 Bad Request` — missing fields. `401 Unauthorized` — invalid credentials. `403 Forbidden` — account deactivated. |
| POST | `/api/auth/logout` | Clears the server-side session so the user is no longer authenticated. Exists to support the logout action in the Part 3 MVC front end. | Any | None | `200 OK` — `{ message: "Session ended." }`. `401 Unauthorized` — no active session. |
| GET | `/api/auth/me` | Returns the identity and role held in the current session. Used by the MVC layer to build role-aware navigation without re-reading the database. | Any | None | `200 OK` — `{ userId, fullName, email, role }`. `401 Unauthorized` — no active session. |
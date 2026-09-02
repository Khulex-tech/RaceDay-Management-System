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
| Standard failure codes | `400` validation error, `401` no active session, `403` wrong role or not the owner, `404` resource does not exist, `409` business-rule conflict (duplicate email, duplicate enrolment). |
| Enrolment status values | `Pending`, `Confirmed`, `Cancelled` (stored on `Enrolments.EnrolmentStatus`). |
| Event type values | `Run`, `Walk`, `Cycle` (stored on `EventTypes.Name`). |
| Dates and times | ISO 8601 in UTC (`2026-11-01T06:00:00Z`). Finish times use `hh:mm:ss`. |
### Role column key

- **None** — public, no session required.
- **Any** — any authenticated user (Organiser or Participant).
- **Organiser** / **Participant** — that role only.

---

## 2. Authentication

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
| --- | --- | --- | --- | --- | --- |
| POST | `/api/auth/register` | Creates a new user account and assigns the role chosen at sign-up (Organiser or Participant). Hashes the password before saving. Exists so that both user types self-register through one endpoint. | None | `{ "firstName", "lastName", "email", "password", "confirmPassword", "phoneNumber", "role" }` — `phoneNumber` is optional. | `201 Created` — `{ userId, firstName, lastName, email, role }`. `400 Bad Request` — validation failed (weak password, passwords do not match, invalid role). `409 Conflict` — email already registered. |
| POST | `/api/auth/login` | Authenticates a user against the stored password hash and creates the server-side session holding `UserId` and `Role`. Exists as the single entry point to an authenticated state. | None | `{ "email", "password" }` | `200 OK` — `{ userId, fullName, role }` plus session cookie. `400 Bad Request` — missing fields. `401 Unauthorized` — invalid credentials. `403 Forbidden` — account deactivated. |
| POST | `/api/auth/logout` | Clears the server-side session so the user is no longer authenticated. Exists to support the logout action in the Part 3 MVC front end. | Any | None | `200 OK` — `{ message: "Session ended." }`. `401 Unauthorized` — no active session. |
| GET | `/api/auth/me` | Returns the identity and role held in the current session. Used by the MVC layer to build role-aware navigation without re-reading the database. | Any | None | `200 OK` — `{ userId, fullName, email, role }`. `401 Unauthorized` — no active session. |
---

## 3. User Profile

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/users/profile` | Returns the full profile of the logged-in user, resolved from the session rather than a URL id so one user can never read another's profile. | Any | None | `200 OK` — `{ userId, firstName, lastName, email, phoneNumber, dateOfBirth, city, profilePictureUrl, role, createdAt }`. `401 Unauthorized` — no active session. |
| PUT | `/api/users/profile` | Updates the logged-in user's own personal details. Email and role changes are validated; the password is not changed here. | Any | `{ "firstName", "lastName", "phoneNumber", "dateOfBirth", "city" }` | `200 OK` — updated profile. `400 Bad Request` — validation failed. `401 Unauthorized` — no active session. |
| PUT | `/api/users/profile/password` | Changes the logged-in user's password after verifying the current one, then hashes and stores the new one. | Any | `{ "currentPassword", "newPassword", "confirmNewPassword" }` | `200 OK` — `{ message: "Password updated." }`. `400 Bad Request` — new passwords do not match or fail policy. `401 Unauthorized` — current password incorrect or no session. |
| POST | `/api/users/profile/picture` | Uploads the user's profile picture, stores it in Azure Blob Storage and saves the returned URL. Planned here so Part 3 file handling goes through the API only. | Any | `multipart/form-data` — `file` (jpg/png, max 5 MB) | `200 OK` — `{ profilePictureUrl }`. `400 Bad Request` — unsupported type or file too large. `401 Unauthorized` — no active session. |
| GET | `/api/users/{id}` | Returns a limited public view of a user (name, city, role). Used by Organisers when reviewing enrolments for their events. | Organiser | None | `200 OK` — `{ userId, fullName, city, role }`. `401 Unauthorized`. `403 Forbidden` — caller is not an Organiser. `404 Not Found` — user does not exist. |

---
## 4. Events

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
| --- | --- | --- | --- | --- | --- |
| GET | `/api/events` | Returns all events with optional filtering and paging, so Participants can browse upcoming races on the Part 3 home page. | None | None — query string: `?eventType=Run&province=Gauteng&fromDate=&toDate=&search=&page=1&pageSize=10` | `200 OK` — `{ page, pageSize, totalCount, items: [ { eventId, name, eventDate, location, province, distanceKm, eventType, entryFee, bannerImageUrl, enrolmentCount } ] }`. `400 Bad Request` — invalid filter or paging values. |
| GET | `/api/events/{id}` | Returns full detail for a single event, including its organiser and available categories, for the event detail page. | None | None | `200 OK` — `{ eventId, name, description, eventDate, location, province, distanceKm, eventType, entryFee, maxParticipants, bannerImageUrl, organiser: { userId, fullName }, categories: [ ... ] }`. `404 Not Found` — event does not exist. |
| GET | `/api/events/upcoming` | Returns only events with a date in the future, ordered by soonest first. Exists to keep the Participant home page query simple and fast. | None | None — query string: `?take=10` | `200 OK` — list of upcoming event summaries. |
| GET | `/api/events/my-events` | Returns the events created by the logged-in Organiser with enrolment counts, powering the Organiser dashboard. | Organiser | None | `200 OK` — `[ { eventId, name, eventDate, distanceKm, enrolmentCount } ]`. `401 Unauthorized`. `403 Forbidden` — caller is a Participant. |
| POST | `/api/events` | Creates a new event owned by the logged-in Organiser, capturing name, description, date, location, distance and event type. | Organiser | `{ "name", "description", "eventDate", "location", "province", "distanceKm", "eventType", "entryFee", "maxParticipants" }` | `201 Created` — created event with `Location` header `/api/events/{id}`. `400 Bad Request` — validation failed (past date, distance ≤ 0, invalid event type). `401 Unauthorized`. `403 Forbidden` — caller is not an Organiser. |
| PUT | `/api/events/{id}` | Updates an existing event. Only the Organiser who created it may update it, enforced by comparing the session `UserId` to `OrganiserId`. | Organiser (owner) | `{ "name", "description", "eventDate", "location", "province", "distanceKm", "eventType", "entryFee", "maxParticipants" }` | `200 OK` — updated event. `400 Bad Request` — validation failed. `401 Unauthorized`. `403 Forbidden` — Organiser does not own this event. `404 Not Found`. |
| DELETE | `/api/events/{id}` | Deletes an event and its categories. Blocked when confirmed enrolments exist so Participant history is never silently lost. | Organiser (owner) | None | `204 No Content` — deleted. `401 Unauthorized`. `403 Forbidden` — not the owner. `404 Not Found`. `409 Conflict` — event has active enrolments. |
| POST | `/api/events/{id}/banner` | Uploads an event banner image to Azure Blob Storage through the API and saves the URL against the event (Part 3). | Organiser (owner) | `multipart/form-data` — `file` (jpg/png, max 5 MB) | `200 OK` — `{ eventId, bannerImageUrl }`. `400 Bad Request` — unsupported type or too large. `401 Unauthorized`. `403 Forbidden` — not the owner. `404 Not Found`. |
| GET | `/api/event-types` | Returns the fixed list of event types (Run, Walk, Cycle) for dropdowns, so the front end never hard-codes them. | None | None | `200 OK` — `[ { eventTypeId, typeName } ]`. |

---
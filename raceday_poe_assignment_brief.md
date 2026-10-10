# Portfolio of Evidence (PoE) – RaceDay Event Management System

## Background
South Africa has a rich road events culture, from the iconic Comrades Marathon between Pietermaritzburg and Durban, to the Cape Town Cycle Tour, the Soweto Marathon, the Two Oceans, and hundreds of community walks, park runs, and charity cycling events held in towns and cities across the country every weekend. Despite the enormous participation these events attract, many are still managed through paper-based registration, spreadsheets, and disconnected communication channels, leaving organisers overwhelmed and participants underserved.

You have been tasked with building **RaceDay**, a full-stack web-based event management system designed specifically for the South African road running, walking, and cycling community. The platform will allow Event Organisers to create and manage events, categories, and participant results, while Participants can browse upcoming events, enter events, track their personal performance history, and prepare for race day using live weather and route information.

This is an individual project built progressively across three parts. By the final submission, you will have produced a fully containerised, cloud-aware, API-driven platform that reflects real-world software development practices used in the sports technology industry.

---

## Important Notes

### Submission
- Submit your GitHub repository link on ARC for each part.
- **No ZIP files are accepted.** All source code must be committed and pushed to GitHub.
- Only working, compiling, and runnable code will be marked.
- A `README.md` file must be included with clear setup instructions, a YouTube video link, and a screenshot of a successful CI/CD green build.

### GitHub and Version Control
- This POE is submitted through GitHub. You will receive a GitHub link from your lecturer—one link for all parts. Use this link to submit your project.
- The lecturer will check the commit history in the repository to verify your contributions. A minimum of **20 meaningful commits per part** is required.
- Commits must reflect real work—commits such as "fixed typo" or "updated file" with no meaningful code change will not count toward the minimum.
- The GitHub Actions CI/CD workflow file must be stored in `.github/workflows/` and must show a green check mark by the submission deadline.

### Video Presentation
- Every part requires a video presentation where you demonstrate and explain your work.
- The video must show the running application and include a voiceover clearly explaining the code structure, logic, and design decisions.
- **No AI-generated voices are permitted** in any video submission.
- Submit all videos as unlisted YouTube links included in the `README.md` file.

### Roles
The system must support two (2) distinct user roles throughout all three parts:
1. **Organiser:** Can create, edit, and delete events, manage event categories, capture participant results, and view all event enrolments.
2. **Participant:** Can create an account, browse events, enter an event by selecting a category, view their own enrolments, and track their personal results.

Role-based access must be enforced at the API level in Part 2 and reflected consistently in the MVC interface in Part 3.

---

## Introduction
This Portfolio of Evidence (POE) consists of three (3) parts, each building directly on the previous one. All parts must be carefully preserved as later parts depend on earlier work.

* **Part 1:** Plan the system by producing an ERD, a full API endpoint plan, and a SQL database script. *No API code is written in this part.*
* **Part 2:** Build the RESTful API in C# as per the approved plan, connect it to the database, and write unit tests with GitHub CI/CD.
* **Part 3:** Build the MVC web application that consumes the API, integrate Azure Blob Storage, and containerise the application using Docker.

> **Tip:** Review all three parts before starting Part 1. Knowing the full picture from the beginning will help you design the database and API correctly and avoid rework later.

---

## Part 1 - System Planning and Database (100 Marks)

### Learning Outcomes
At the end of this part, students should be able to:
- Design a relational database using an Entity Relationship Diagram (ERD).
- Plan a RESTful API using a structured endpoint specification table.
- Write a SQL script to create and populate a database schema using SSMS.
- Demonstrate understanding of role-based system design before writing any application code.

### Description
Before writing a single line of application code, you will plan the entire system. This planning is not optional; it is assessed and must be submitted as part of this part. All planning documents must be committed to your GitHub repository inside a `/docs` folder before any API code is written in Part 2.

#### Section A - Entity Relationship Diagram (ERD)
- Draw an ERD showing all database entities, their attributes, and the relationships between them.
- The ERD must reflect the full data model for the RaceDay system with a minimum of **six (6) entities**.
- Clearly indicate primary keys, foreign keys, and cardinality for every relationship (one-to-many, many-to-many).
- Submit as a PNG or PDF inside the `/docs` folder.
- Your SQL script in Section C must match your ERD exactly. Any deliberate differences must be explained in the README.

#### Section B - API Endpoint Plan
Create a table listing every API endpoint your system will expose. Refer to the Functional Requirements in Part 2 to ensure all functionality is covered.

The table must include the following columns:
* **HTTP Method:** The type of HTTP request (`GET`, `POST`, `PUT`, `DELETE`).
* **Route:** The URL path (starts with `/api/`).
* **Description:** Purpose of the endpoint.
* **Role Required:** Options: None (public), Any (logged in), or specific role (`Organiser`, `Participant`).
* **Request Body:** JSON fields sent (if any).
* **Expected Response:** HTTP status code and short description of return format / failure codes.

Plan endpoints for:
1. Authentication (register & login)
2. User Profile
3. Events
4. Categories
5. Event Enrolments
6. Results

##### Endpoint Plan Table Example
*(Provided for formatting context only)*

| HTTP Method | Route | Description | Role Required | Request Body | Expected Response |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `POST` | `/api/books/{id}/borrow` | Records a borrow request for a specific book. Links the logged-in member to the book and sets the due date. | Member | `{ "borrowDate", "returnDate" }` | `201 Created` - borrow record with due date.<br>`404 Not Found` - book does not exist.<br>`409 Conflict` - book already on loan. |

#### Section C - SQL Database Script
- Write a SQL script that creates the full database schema for the RaceDay system using SQL Server Management Studio (SSMS).
- Include `CREATE TABLE` statements for every entity in your ERD.
- All primary keys, foreign keys, and constraints (`NOT NULL`, `UNIQUE`, `DEFAULT`) must be defined.
- Include `INSERT` statements to seed realistic sample data: at minimum two (2) Organisers, two (2) Participants, three (3) Events, categories for each event, and sample enrolments.
- Save as a `.sql` file and commit to `/docs`. Test on a clean instance before submission.

#### GitHub and CI/CD
- All planning documents and SQL script inside `/docs`.
- Minimum 20 meaningful commits.
- Configure a GitHub Actions workflow that validates repository structure (e.g., checks `/docs` folder existence). Include screenshot in `README.md`.

---

## Part 2 - RESTful API Development (100 Marks)

### Learning Outcomes
At the end of this part, students should be able to:
- Build a RESTful API using ASP.NET Core Web API in C#.
- Connect the API to a SQL Server database using Entity Framework Core.
- Implement role-based session authentication for Organiser and Participant roles.
- Write and run unit tests for API endpoints.
- Configure automated CI/CD using GitHub Actions.

### Requirements

1. **Project Setup:** ASP.NET Core Web API project in Visual Studio connected to SQL Server via EF Core Code-First. Schema must match Part 1.
2. **Authentication and Role-Based Access:**
   - Registration and login with role selection (`Organiser` / `Participant`).
   - Session management to maintain state and role.
   - Non-public areas restricted to authenticated users; role separation strictly enforced.
   - Passwords must be securely hashed.
3. **Functional Requirements:**
   - **Authentication:** Register & Login.
   - **User Profile:** View/Update own details.
   - **Events:** Organisers manage (CRUD); both roles view. Capture name, description, date, location, distance, and event type (run, walk, cycle).
   - **Categories:** Organisers define age/distance categories per event; both roles view.
   - **Event Enrolments:** Participants enter events by category; Organisers view all event enrolments.
   - **Results:** Organisers capture finish times/positions; Participants view personal results.
4. **Swagger Integration:** Integrated for live testing and browser verification. Each endpoint must include descriptions, input format, and output status codes.
5. **Unit Testing:** Unit tests using frameworks like xUnit/NUnit testing success/failure paths, registration/login, event management, role authorization rejection, and enrolment handling.
6. **GitHub and CI/CD:** Minimum 20 commits. GitHub Actions workflow building and running unit tests on every push.

---

## Part 3 - Automation of Web Application (100 Marks)

### Learning Outcomes
At the end of this part, students should be able to:
- Build an ASP.NET Core MVC web application that consumes a RESTful API.
- Implement session-based authentication and role-specific interfaces for Organiser and Participant users.
- Integrate Azure Blob Storage into the API for cloud-based file storage.
- Containerise an ASP.NET Core application using Docker.

### Requirements

1. **MVC Application Setup:** ASP.NET Core MVC consuming the Part 2 API. No direct database access or duplicated business logic.
2. **Authentication & Role-Based Navigation:**
   - Register/Login screens communicating with Part 2 API.
   - State/role maintained via session.
   - Distinct navigation menus per role; automatic redirects for unauthorized users.
3. **Organiser Interface:**
   - Dashboard with summary metrics (events, total enrolments, upcoming dates).
   - Event Management (CRUD) with event banner upload capabilities.
   - Category Management.
   - Enrolments view.
   - Results entry and publication form.
4. **Participant Interface:**
   - Home page with searchable/filterable events.
   - Event details page with entry functionality.
   - Personal Enrolments view ("My Events").
   - "My Results" history page showing race history, finish times, and relative placement.
   - Profile page with picture upload.
5. **Azure Blob Storage:**
   - Integrated inside the **Part 2 API** (MVC application passes files through API).
   - Used for Event Banners and User Profile Pictures.
6. **Docker:**
   - Clean, multi-stage `Dockerfile`.
   - Provide exact Docker run commands in `README.md`.
7. **UI Design:**
   - Energetic sporting visual theme, Bootstrap/Tailwind layout, responsive components.
   - Visual status badges (e.g., green for Confirmed, amber for Pending).
   - Graceful error messaging.
8. **GitHub and CI/CD:** Minimum 20 commits. CI pipeline updated to build both API and MVC projects.

---

## Evaluation Rubrics

### Part 1 Rubric (100 Marks)

| Criteria | Does not meet standard | Meets standard | Partially exceeds standard | Greatly exceeds standard |
| :--- | :--- | :--- | :--- | :--- |
| **Correct Submission**<br>*(5 Marks)* | Missing files, no README, or `/docs` folder absent.<br>*(0–1 Marks)* | Required files present with a basic README.<br>*(2–3 Marks)* | Organised structure. README covers system description and roles. `/docs` folder present.<br>*(4 Marks)* | Clear folder structure. Detailed README with setup notes. All required documents in `/docs`. CI screenshot and video link included.<br>*(5 Marks)* |
| **ERD**<br>*(25 Marks)* | No ERD submitted or ERD is critically incomplete.<br>*(0–8 Marks)* | ERD covers all required entities with relationships and keys shown. Minor cardinality issues.<br>*(9–16 Marks)* | Complete ERD with all required entities, attributes, primary/foreign keys, and relationship types.<br>*(17–21 Marks)* | Complete and accurate ERD with all entities, attributes, primary/foreign keys, correct cardinality. Matches SQL script exactly.<br>*(22–25 Marks)* |
| **API Endpoint Plan**<br>*(25 Marks)* | No plan submitted or fewer than 5 endpoints listed.<br>*(0–8 Marks)* | Basic plan with method and route only. Most details missing.<br>*(9–16 Marks)* | Plan covers all required resources with method, route, role, and description completed.<br>*(17–21 Marks)* | Comprehensive plan with all required endpoints. All six columns completed. Clear request bodies and response codes.<br>*(22–25 Marks)* |
| **SQL Database Script**<br>*(20 Marks)* | No script submitted or script does not run.<br>*(0–6 Marks)* | Script runs but missing foreign keys, constraints, or seed data.<br>*(7–12 Marks)* | Script creates all tables with keys and constraints. Basic seed data included.<br>*(13–16 Marks)* | Script runs cleanly on a fresh instance. Tables match ERD. Constraints defined. Realistic seed data covers all entities.<br>*(17–20 Marks)* |
| **GitHub & CI/CD**<br>*(15 Marks)* | No commits or CI workflow not present.<br>*(0–5 Marks)* | Fewer than 15 commits. CI workflow present.<br>*(6–10 Marks)* | 15 commits made. CI workflow runs successfully.<br>*(11–12 Marks)* | 20+ meaningful commits. CI workflow validates project successfully.<br>*(13–15 Marks)* |
| **Video Presentation**<br>*(10 Marks)* | No video submitted or no explanation.<br>*(0–3 Marks)* | Video covers basic features with limited explanation.<br>*(4–6 Marks)* | Clear video demonstrating key features with good explanation.<br>*(7–8 Marks)* | Engaging, professional video explaining ERD, endpoints, and live SQL script execution in SSMS.<br>*(9–10 Marks)* |

---

### Part 2 Rubric (100 Marks)

> **Note:** Incorrect or absent GitHub usage results in a **-5% penalty**.

| Criteria | Does not meet standard | Meets standard | Partially exceeds standard | Greatly exceeds standard |
| :--- | :--- | :--- | :--- | :--- |
| **Correct Submission**<br>*(5 Marks)* | Missing files or no README.<br>*(0–1 Marks)* | Required files and basic README present.<br>*(2–3 Marks)* | Organised structure. README covers setup and run instructions.<br>*(4 Marks)* | Clear folder structure. Detailed README with full setup steps, CI screenshot, and video link.<br>*(5 Marks)* |
| **GitHub & CI/CD**<br>*(10 Marks)* | No commits or CI workflow not present.<br>*(0–2 Marks)* | Fewer than 15 commits. CI workflow present.<br>*(3–5 Marks)* | 15 commits made. CI workflow runs successfully.<br>*(6–8 Marks)* | 20+ meaningful commits. CI workflow validates repository with green build.<br>*(9–10 Marks)* |
| **Auth & Role Access**<br>*(20 Marks)* | No auth or roles implemented.<br>*(0–6 Marks)* | Basic login/register with no role enforcement.<br>*(7–12 Marks)* | Both roles implemented. Session created on login. Role enforcement on most endpoints.<br>*(13–17 Marks)* | Fully working auth for both roles. Server-side session state. Protected endpoints enforced. Passwords securely hashed.<br>*(18–20 Marks)* |
| **API Endpoints & DB**<br>*(25 Marks)* | Few/no endpoints. No DB connection.<br>*(0–8 Marks)* | Basic endpoints with working DB for 1–2 resources.<br>*(9–16 Marks)* | Most planned endpoints functional. EF Core and session-based access used.<br>*(17–21 Marks)* | All planned endpoints implemented correctly with full CRUD where required. EF Core used. Schema matches Part 1.<br>*(22–25 Marks)* |
| **Swagger Integration**<br>*(10 Marks)* | No Swagger or app doesn't run.<br>*(0–3 Marks)* | Swagger included but endpoints not all visible or testable.<br>*(4–6 Marks)* | Swagger functional; most endpoints described and testable.<br>*(7–8 Marks)* | All endpoints visible/testable in Swagger UI. Clear descriptions of inputs, outputs, and responses.<br>*(9–10 Marks)* |
| **Unit Testing**<br>*(15 Marks)* | No tests or tests fail.<br>*(0–5 Marks)* | 1–2 basic passing tests.<br>*(6–10 Marks)* | Tests cover auth, role enforcement, and at least two resources.<br>*(11–13 Marks)* | Comprehensive test suite covering expected success and failure scenarios. All tests pass locally and in CI.<br>*(14–15 Marks)* |
| **Video Presentation**<br>*(15 Marks)* | No video submitted or no explanation.<br>*(0–5 Marks)* | Video covers basic features with limited explanation.<br>*(6–10 Marks)* | Clear video demonstrating key features with good explanation.<br>*(11–13 Marks)* | Professional video covering all endpoints, authentication, Swagger UI, unit tests, and code structure.<br>*(14–15 Marks)* |

---

### Part 3 Rubric (100 Marks)

> **Note:** Incorrect or absent GitHub usage results in a **-5% penalty**.

| Criteria | Does not meet standard | Meets standard | Partially exceeds standard | Greatly exceeds standard |
| :--- | :--- | :--- | :--- | :--- |
| **Correct Submission**<br>*(5 Marks)* | Missing files or no README.<br>*(0–1 Marks)* | Required files and basic README present.<br>*(2–3 Marks)* | Organised structure. README covers Docker instructions and feature descriptions.<br>*(4 Marks)* | Clear structure. Complete README with Docker run command, CI screenshot, and video link.<br>*(5 Marks)* |
| **GitHub & CI/CD**<br>*(10 Marks)* | No commits or CI workflow not present.<br>*(0–2 Marks)* | Fewer than 15 commits. CI workflow present.<br>*(3–5 Marks)* | 15 commits made. CI workflow runs successfully.<br>*(6–8 Marks)* | 20+ meaningful commits. CI workflow builds both API and MVC with green build.<br>*(9–10 Marks)* |
| **Auth & Navigation**<br>*(10 Marks)* | No auth pages or role handling.<br>*(0–2 Marks)* | Auth present but lacks role-based navigation.<br>*(3–5 Marks)* | Functional auth. Session created. Basic role-based navigation present.<br>*(6–8 Marks)* | Fully functional register/login. Session handles ID & role. Distinct menus, logout clearing, and guest redirects.<br>*(9–10 Marks)* |
| **Organiser Interface**<br>*(15 Marks)* | Organiser pages missing or broken.<br>*(0–5 Marks)* | 1–2 Organiser pages functional.<br>*(6–10 Marks)* | Most pages functional (Events, Categories, Enrolments).<br>*(11–13 Marks)* | All Organiser pages fully functional: Dashboard, Events, Categories, Enrolments, and Results entry.<br>*(14–15 Marks)* |
| **Participant Interface**<br>*(15 Marks)* | Participant pages missing or broken.<br>*(0–5 Marks)* | 1–2 Participant pages functional.<br>*(6–10 Marks)* | Most pages functional (Event browsing, Entry, My Events).<br>*(11–13 Marks)* | All pages functional: Home, Event Detail/Entry, My Events, My Results with finish times/ranks, and Profile.<br>*(14–15 Marks)* |
| **Azure Blob Storage**<br>*(10 Marks)* | No Blob Storage integration.<br>*(0–2 Marks)* | Blob Storage connected but uploads fail.<br>*(3–5 Marks)* | Uploads saved to Azure and URLs saved in database.<br>*(6–8 Marks)* | Full API-mediated file handling. Files uploaded via MVC through API to Azure and displayed correctly.<br>*(9–10 Marks)* |
| **Docker Container**<br>*(10 Marks)* | No Dockerfile or container fails to start.<br>*(0–2 Marks)* | Dockerfile present; application partially runs.<br>*(3–5 Marks)* | Application runs in Docker with minor issues.<br>*(6–8 Marks)* | Container builds and runs cleanly. Working Docker run command in README.<br>*(9–10 Marks)* |
| **UI Design**<br>*(10 Marks)* | Cluttered, inconsistent, or broken UI.<br>*(0–2 Marks)* | Basic UI with minimal styling.<br>*(3–5 Marks)* | Clean UI with consistent layout, sporting theme, and role-based menus.<br>*(6–8 Marks)* | Polished, professional UI reflecting the RaceDay brand. Colour-coded status badges, good imagery, intuitive layout.<br>*(9–10 Marks)* |
| **Video Presentation**<br>*(15 Marks)* | No video submitted or no explanation.<br>*(0–5 Marks)* | Video covers basic features with limited explanation.<br>*(6–10 Marks)* | Clear video demonstrating key features with good explanation.<br>*(11–13 Marks)* | Thorough video showing full app, MVC structure, API interaction, Azure Blob uploads, and Docker execution.<br>*(14–15 Marks)* |

---

### Total Assessment Marks: 300
```

I have created the requested Markdown document containing the complete `PROG6212` Portfolio of Evidence briefing, functional requirements, and all three assessment rubrics.
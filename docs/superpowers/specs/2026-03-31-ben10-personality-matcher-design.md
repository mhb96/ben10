# Ben 10 Character Personality Matcher — Design Spec

**Date:** 2026-03-31
**Status:** Approved

---

## Overview

A responsive web application that assigns a Ben 10 alien (original series) to a user based on their personality, delivered through a quiz-style experience. Results are persisted to a MSSQL database and users can generate a shareable result card.

**Target audience:** Teenagers and young adults.

---

## Stack

| Layer | Technology |
|---|---|
| Frontend | React (Vite) |
| Shareable card generation | `html2canvas` (client-side) |
| Backend | ASP.NET Core Web API |
| Database | Microsoft SQL Server (MSSQL) |
| Backend unit tests | xUnit |

---

## Architecture

```
[User Browser]
     |
  React SPA (Vite)
     |  REST/JSON
  ASP.NET Core Web API
     |
  SQL Server (MSSQL)
```

- The React frontend is a single-page application served statically.
- All matching logic lives exclusively in the ASP.NET Core backend.
- The frontend submits answers and receives a matched character + result ID.
- Shareable cards are generated entirely client-side using `html2canvas` — no server-side image generation.

---

## Frontend Screens

1. **Home** — Brief intro, "Start Quiz" CTA button.
2. **Quiz** — One question rendered at a time, progress bar showing question N of total.
3. **Loading** — Transition screen displayed while awaiting backend response.
4. **Result** — Matched alien name, image, personality description, traits that matched, shareable card download button, "Retake" button.

### Error Handling (Frontend)
- If the backend is unreachable, the Loading screen shows a friendly error message and a "Try Again" button.
- If the API returns a `400`, the user is prompted to complete the quiz before submitting.
- No raw error details or stack traces are shown to the user.

---

## Backend

### Endpoints

| Method | Route | Description |
|---|---|---|
| `GET` | `/api/quiz/questions` | Returns the list of quiz questions and answer options |
| `POST` | `/api/quiz/submit` | Accepts array of answers, returns matched character + result ID |
| `GET` | `/api/results/{id}` | Returns a saved result by GUID (for shareable links) |

### Question Data
Questions and their trait mappings are stored as a static file: `Data/questions.json` in the ASP.NET Core project. This file is versioned with the codebase. No database table for questions.

**Question structure (JSON):**
```json
{
  "id": 1,
  "text": "When faced with a problem, you...",
  "answers": [
    { "text": "Charge straight in", "traits": { "impulsive": 2, "brave": 1 } },
    { "text": "Think it through first", "traits": { "strategic": 2, "intelligent": 1 } },
    { "text": "Look for a creative workaround", "traits": { "creative": 2, "adaptable": 1 } },
    { "text": "Ask others for help", "traits": { "empathetic": 2, "teamwork": 1 } }
  ]
}
```

### Matching Algorithm
- Each answer maps to one or more personality traits with a weight.
- Each Ben 10 alien (original series) has a weighted trait profile.
- On submission, the backend sums trait scores from all answers.
- The alien with the highest total score is returned as the match.
- In the case of a tie, the first alien in the profile list wins (deterministic).

**Example alien profile:**
```json
{
  "name": "Heatblast",
  "traits": { "impulsive": 3, "brave": 2, "hot-headed": 3, "passionate": 1 },
  "description": "You're passionate, bold, and never back down from a challenge. You act on instinct and your fiery energy is impossible to ignore.",
  "image": "/images/heatblast.png"
}
```

**Original series alien roster (10 aliens):**
Heatblast, Wildmutt, Diamondhead, XLR8, Greymatter, Four Arms, Stinkfly, Ripjaws, Upgrade, Ghostfreak.

### Error Handling (Backend)
- Incomplete or malformed submissions return `400 Bad Request` with a descriptive message.
- All unhandled exceptions return `500 Internal Server Error` with a generic message — no stack traces exposed.

---

## Database

### Table: `QuizResults`

| Column | Type | Notes |
|---|---|---|
| `Id` | `UNIQUEIDENTIFIER` PK | GUID, generated on insert |
| `SessionId` | `UNIQUEIDENTIFIER` | Client-generated session identifier |
| `MatchedCharacter` | `NVARCHAR(100)` | Name of matched alien |
| `AnswersJson` | `NVARCHAR(MAX)` | Raw submitted answers as JSON |
| `TraitScoresJson` | `NVARCHAR(MAX)` | Computed trait scores as JSON |
| `CreatedAt` | `DATETIME2` | UTC timestamp of result creation |

---

## Shareable Card

- Generated client-side in React using `html2canvas`.
- The card renders: alien name, image, short description, and a tagline (e.g., "I got Heatblast on the Ben 10 Personality Quiz!").
- Exported as a downloadable PNG.
- The result URL (`/results/{id}`) is also shareable — it fetches the result from `GET /api/results/{id}` and renders the result screen.

---

## Testing

### Backend (xUnit)
- **Matching algorithm unit tests** — given a fixed set of answers, assert the correct alien is returned. Cover edge cases: tie-breaking, all-neutral answers, single-trait answers.
- **API integration tests** — `POST /api/quiz/submit` with a known answer set; assert correct character returned and result persisted to DB. `GET /api/results/{id}` with a valid ID; assert correct data returned.

### Frontend
- Manual testing of full quiz flow, result card generation, and download for v1.
- No automated UI tests in scope for v1.

---

## Out of Scope (v1)

- User accounts / authentication
- Leaderboards or social features beyond the shareable card
- Admin dashboard for managing questions
- Alien series beyond the original (2005)
- Push notifications or email sharing

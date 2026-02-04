# FairPlay Football API

## Quick Start

### Local Development
1. **Start PostgreSQL**:
   ```bash
   docker-compose up -d
   ```
2. **Apply Migrations**:
   ```bash
   cd backend
   dotnet ef database update
   ```
3. **Run API**:
   ```bash
   dotnet watch run
   ```

### API Endpoints
- **Matches**:
  - `POST /api/matches`: Create a match.
  - `POST /api/matches/calculate-teams`: Balance teams (Snake Draft).
  - `PATCH /api/matches/{id}/complete`: Process ratings and close match.
- **Ratings**:
  - `POST /api/ratings/submit`: Bulk submit match ratings.

### Formula
Rating Smoothing: `NewRating = (CurrentRating * 0.8) + (WeeklyAverage * 0.2)`

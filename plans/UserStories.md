# User Roles & Stories

## 1. Super Admin
The aim of the Super Admin role is to create and maintain league status, as well as adding the initial player to a league who will be created as the League Admin.

**As a Super Admin, I should be able to:**
- Login to the app.
- View all leagues in the system.
- Add new leagues and deactivate existing ones.
- Add a new user who will be created as the Admin for a specific league.
- **Restriction:** I should *not* be able to view matches, players, or league-specific content unless I am also a member of that league.

## 2. League Admin
The aim of the League Admin role is to maintain the day-to-day running of a league.

**As a League Admin, I should be able to:**
- Add new players to the league:
    - By entering the email of an existing system user (linking them to the league).
    - By creating a new user account (who becomes a player in the league).
- Create matches.
- Add players to matches and generate balanced teams.
- Manually update ratings for players within their league.
- Send push notifications to users via the PWA (e.g., match generation alerts).
- Promote a regular player to a League Admin.
- Act as a player in the league (participate in matches, rate others).

## 3. Player
A Player is a user who plays in the league with no administrative privileges.

**As a Player, I should be able to:**
- Maintain my profile.
- View matches I am playing in.
- Rate other players in matches I have played in.
- Have multiple player profiles (one for each league I join).

**Note:** If a user exists in the system, a League Admin can add them by email. This creates a new **Player Profile** linked to that User and League.


# Website: adding a game

The studio site is [renegade-games.com](https://renegade-games.com), served by GitHub Pages from
the [renegade-games-site](https://github.com/darren-rawlings/renegade-games-site) repository.
DNS stays at GoDaddy (four `A` records for `@` pointing at GitHub, `www` as a CNAME to
`darren-rawlings.github.io`); the `MX`/`TXT` records run the support@ mailbox and must be left alone.

## New game

1. Add a screenshot to `images/<game>.jpg` (720 px wide is plenty, JPEG quality ~82).
2. In `index.html`, copy the `<article class="game">` block of an existing game into the
   `.games` list (newest first) and change the image, title, description and tags.
3. Until the game is live, use the "coming soon" button:
   `<span class="store soon">Google Play: coming soon</span>`
4. When it's live, swap it for the real link:
   `<a class="store" href="https://play.google.com/store/apps/details?id=com.renegadegames.<game>">Get it on Google Play</a>`
5. Commit and push; Pages republishes in about a minute.

## Shared pages

- `privacy.html` covers every game. Update it (and its date) if a game collects anything new,
  adds accounts or purchases, or uses a different ad setup.
- `app-ads.txt` has one line per ad network account, not per game: nothing to add for a new game
  that uses the same AdMob account.

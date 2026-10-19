# Release playbook

From "the game works on my phone" to "live on Google Play", in order. Written from shipping
Counter Fire; the **Gotchas** are things that actually cost us time.

## 1. Before the first build that leaves your PC

- [ ] **Name.** Search Google, the Play Store and [USPTO](https://tmsearch.uspto.gov) (class 009)
      for the title. Don't use another company's trademark in it ("Connect Four" is Hasbro's).
- [ ] **Package name** in `export_presets.cfg`: `com.renegadegames.<gamename>`. It becomes
      **permanent** at the first Play upload.
- [ ] **Launcher name** (`package/name`) and the in-game title match the store name.
- [ ] **Audience** decided: children included or not. If children are included:
      `AdMobConfig(ForChildren: true)` (the default), and remove the advertising ID permission
      (step 4).
- [ ] **Icon**: SVG sources in `assets/icon/`, rendered to the adaptive layers
      (`background_432.png`, `foreground_432.png` with the art inside a 132 px radius of the centre,
      `monochrome_432.png`), `icon_192.png` and the Play Store `icon_512.png`. See
      `docs/NEW_GAME.md` for the render commands.

## 2. AdMob

- [ ] In [AdMob](https://admob.google.com): **Apps → Add app** (Android, not listed yet).
- [ ] Create the ad units the game uses (usually **Interstitial** "Between games" and **Rewarded**).
- [ ] Put the **App ID** in Project Settings → `admob/general/android/app_id`.
- [ ] Put the unit IDs in the game's `AdMobConfig` (replacing `AdMobConfig.Test`).
- [ ] Add the publisher line to `app-ads.txt` on the website (one line covers every game):
      `google.com, pub-XXXXXXXXXXXXXXXX, DIRECT, f08c47fec0942fa0`
- [ ] Never tap your own live ads. Test with test ads, or register your phone as a test device.

## 3. Release key (once per game)

```
C:\GameDev\Android\jdk-17\bin\keytool.exe -genkeypair -v -keystore C:\GameDev\Android\keystores\<game>-release.keystore -alias <game> -keyalg RSA -keysize 2048 -validity 10000
```

- [ ] Store the password in the README next to the keystore, and **back up the whole
      `keystores` folder** off this PC. Never commit it.
- [ ] Godot: Editor Settings → Export → Android, or the export preset's Release keystore fields.

## 4. Release build (.aab)

- [ ] `use_gradle_build` in the Android preset (the build script switches the export to AAB).
- [ ] Bump `version/code` (+1 every upload) and `version/name`.
- [ ] `powershell -File C:\GameDev\renegade-core\tools\Build-Release.ps1 -Project <game folder>` builds
      `build\android\<Name>.aab`. It installs the build template if needed, removes the advertising
      ID permissions the ads SDK adds (children's games must not send it; the template is
      regenerated, so it does this on every build) and signs with the key from
      `C:\GameDev\Android\keystores`, passing the password in environment variables so it never
      lands in a project file.
- [ ] Install a release APK on the phone and play a full session once more.

## 5. Play Console

Account (once): personal account, identity verified, Android device access via the Play Console
app, phone verified. Contact `support@renegade-games.com`, website `https://renegade-games.com`.

Per game:
- [ ] **Create app**: name, Game, Free (can never become paid).
- [ ] **App content**: privacy policy `https://renegade-games.com/privacy.html`; Ads: yes;
      App access: all functionality available; Content rating questionnaire; Target audience
      (children included → Families policy); Data safety (no data collected by us; AdMob's
      collection declared per Google's AdMob data disclosure); Advertising ID: matches step 4.
- [ ] **Store listing**: short description (80 chars), full description, 512×512 icon,
      1024×500 feature graphic, at least 2 phone screenshots. `tools/Take-Screenshots.ps1` records them
      from the demo (1080 × 1920, no alpha) and `tools/New-FeatureGraphic.ps1` makes the graphic.
- [ ] **Closed testing**: personal accounts need **12 testers opted in for 14 days in a row**
      before production access. Start this early.
- [ ] **Production**: upload the .aab, roll out.

### In-app products (Remove ads, tips)

The games already contain the shop (renegade-core `Store/Shop.cs`, with the
GodotGooglePlayBilling plugin in each game's `addons`). Its buttons stay hidden until Google Play
answers with these products, so nothing shows until they exist. Per game:
- [ ] **Payments profile** (once, for the account): Play Console > Settings > Payments profile.
      Needed before any product can be priced; it's where Google pays you.
- [ ] Upload a build that includes the billing plugin to a testing track first; Play only lets
      you create products for an app that has one.
- [ ] **Monetize > Products > In-app products**, create (IDs must match exactly):
      - `remove_ads`: "Remove all ads", one-time, **$0.99** (Play suggests local prices).
      - `tip_small` ($0.99), `tip_medium` ($2.99), `tip_large` ($4.99): "Tip", one-time;
        the game consumes them so they can be given again. Play has no "any amount": it only
        sells fixed prices, so a few tip sizes stand in for a free choice.
- [ ] Set each product **Active**.
- [ ] **License testing** (Settings > License testing): add your Google account, so test
      purchases are free and can be refunded.
- [ ] Declare in-app purchases in the store listing ("Contains ads · In-app purchases").
- Families policy: the shop is in Settings only, with real prices, no pressure, and "Ask a
  grown-up first"; Google Play's own purchase screen asks for the account password / Family Link
  approval.

## 6. After launch

- [ ] Website: change the game's button from "coming soon" to the Play Store link
      (see `docs/WEBSITE.md`).
- [ ] Tag the release in git: `git tag v1.0.0 && git push --tags`.

## Gotchas

- **No `&` in any path** the Android build touches. `Godot & VS Code` broke Gradle and the
  Android `.bat` tools. Don't rename a folder VS Code has open, either: it broke both Claude Code
  and Source Control.
- **Phone won't show up over USB** (Samsung): Auto Blocker blocks it. Use wireless debugging
  (`adb pair`, then `adb mdns services` for the connect port). Turn Auto Blocker back on after.
- **Git Bash mangles phone paths**: prefix adb commands with `MSYS_NO_PATHCONV=1`.
- **Foldables have two screens**: `adb exec-out screencap -p -d <display id>`
  (ids from `adb shell dumpsys SurfaceFlinger --display-id`).
- **`--demo` uses the stand-in ads**, never AdMob. Test real ads by playing by hand.
- **Phone speakers** lose thin, high or sustained sounds. Check every material on the phone;
  a pure 3 kHz ring all but vanished, and a scrape under a short click hid the click.
- **AdMob plugin loaders**: use a new `*AdLoader` per ad. Reusing one shares an ID between
  ads, so one dismissal fires every past ad's callbacks (Undo undid several moves).
- **First shader use stalls a frame on phones**: draw new effects once at startup, hidden.

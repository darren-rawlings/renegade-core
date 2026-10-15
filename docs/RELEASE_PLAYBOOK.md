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

- [ ] `use_gradle_build` and **Export Format: AAB** in the Android preset.
- [ ] Children included? Remove the advertising ID permission the ads SDK adds:
      in `android/build/AndroidManifest.xml` add
      `<uses-permission android:name="com.google.android.gms.permission.AD_ID" tools:node="remove"/>`
      (and `xmlns:tools="http://schemas.android.com/tools"` on `<manifest>`).
- [ ] Bump `version/code` (+1 every upload) and `version/name`.
- [ ] `Godot.exe --headless --path . --export-release "Android" build/android/<Game>.aab`
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
      1024×500 feature graphic, at least 2 phone screenshots.
- [ ] **Closed testing**: personal accounts need **12 testers opted in for 14 days in a row**
      before production access. Start this early.
- [ ] **Production**: upload the .aab, roll out.

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

# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.1.0] - 2022-05-05

### Added
- Add an editor window to manage borrowed assets, it can be found under Window > Borrit > Borrowed Assets
- In the hierarchy window, indicate on scenes and instances of prefabs if they have been borrowed

### Fixed
- Stop refreshing borrowed assets when editor is out of focus
- Fix Refresh not properly updating anything using GoogleAppScriptDatabase. Please update the GoogleAppScript.gs in your gsheet and redeploy it!

## [2.0.0] - 2022-05-03

### Removed
- **[BREAKING]** Remove GoogleSheets database using the Google API, you can install it via a separate package found [here](https://github.com/rthery/borrit-gsheet)

### Fixed
- Improve handling RefreshDatabaseCoroutine lifetime
- Display error message in Background Tasks window
- Stop spamming errors when disconnected, keep the progress as failed instead in the Background Tasks window
- Improve error handling during initialization
- Handle exception when receiving invalid response data from GoogleAppScript
- Disable Borrit when editor is running in batch mode

## [1.0.1] - 2021-05-05

### Fixed
- All Google Script app URL should now be valid

## [1.0.0] - 2021-04-28

Out of preview! Borrit is ready to be used in production with Google Sheets via Google Script Web App

### Updated
- Google Script Web App is now the preferred way of using Google Sheets as a database
- README has now better explanations on how to use Google Sheets as a database

### Added 
- Borrow and Return actions have keyboard shortcuts (Alt + Shift + B and Alt + Shift + R respectively)
- Add a Refresh entry in the context menu
- Add progress dialog when borrowing and returning
- Add progress of database refresh in the Background Tasks window (Unity 2020.1+)
- Username is by default the same as the one used in Git (if possible)

### Fixed
- Improved update of the Project window when assets have been updated
- Disable Borrow and Return when no username has been input
- Username can now support characters from UTF8
- Requests issued before the database has been reset are ignored
- Fix memory leak caused by UnityWebRequest not being disposed
- Borrow and Return actions are not blocked anymore when there's a refresh in the background

## [0.1.1-preview.1] - 2021-04-27

### Added
- Support for Google Sheets App Scripts Web App, greatly simplifying connection process

## [0.1.0-preview.1] - 2021-01-11

### This is the first release of Borrit.

Borrit is a tool for Unity adding a soft lock system in the Editor Project window
allow you and your team to mark assets as borrowed.

This version is a PoC, at the moment it only supports Google Sheets as a pseudo database to store the state of
borrowed assets, although it has been built to have more databases supported in the future.
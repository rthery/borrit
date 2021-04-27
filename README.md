# Borrit

> Borrit is a tool for Unity adding a soft lock system for assets in the Editor

Working as a team on a Unity project can be troublesome because of potential difficult 
to resolve conflicts on some assets. This tool offers a simple way to borrow/return 
assets from the Project window directly.

It's currently independent from any version control system and try to support different
databases to store the state of the borrowed assets.

![Project window with Borrit](Documentation/images/example.png)

> :warning: **This is a Proof of Concept with support for Google Sheets only, requiring your
> own Google Cloud API application to access it. It works, but A LOT OF THINGS ARE MISSING!**

## Installation
Use the package manager in Unity to install this package from git.

### Google Spreadsheets
#### Project Setup
For the Google Sheets Database, you will need a bit of setup.
1. Go to Google Drive and create a Spreadsheet with at least one sheet sheet in it with the name of your project
   ([Application.productName](https://docs.unity3d.com/ScriptReference/Application-productName.html))
2. Share as Editor with Google accounts who will use Borrit
3. Follow the instructions [here](https://developers.google.com/sheets/api/guides/authorizing) to create
   an application that will allow you to access Google Sheets.
4. Download the OAuth 2.0 credentials JSON file you'll obtained through Google Cloud Console.
5. Place this file somewhere in your project (it needs to be shared with your team, so push it
   to your remote repository), a good place would be in `ProjectSettings\Packages\io.github.borrit`
   beside the other settings of Borrit, if your repository is not public
6. In Unity, Open Borrit settings, Edit > Project Settings > Borrit
7. Select 'Google Sheets' Database
8. Select the credentials.json file in the Credentials field
9. Input the [spreadsheet id](https://developers.google.com/sheets/api/guides/concepts#spreadsheet_id)

#### User Setup
Once the project is setup and credentials.json available somewhere
1. Open Borrit settings, Edit > Project Settings > Borrit
2. Select a username if it's empty
3. Make sure Credentials file is present, otherwise select it
4. Create your authentication token
5. Connect and you should be ready to use Borrit

### Google App Script
This is a much easier method to use Google Sheets as a database, than the previous method.

#### Project Setup
1. Go to Google Drive and create a Spreadsheet
2. Create a Google App Script document, Tools > Script editor
3. Paste the content of [GoogleAppScript.gs](Documentation/GoogleAppScripts/GoogleAppScript.gs)
4. Replace the `#Your spreadsheet id goes here#` at the top of the script with your [spreadsheet id](https://developers.google.com/sheets/api/guides/concepts#spreadsheet_id)
5. Save the script
6. Press Deploy > New Deploy (top right corner of the page). Deploy it as a Web App. Copy the Web app URL that appears on the screen.
7. Select type 'Web App' and select 'Anyone' for 'Who has access'
8. Confirm deploy and copy the Web App URL displayed
9. In Unity, Open Borrit settings, Edit > Project Settings > Borrit
10. Select 'Google App Script' Database
11. Paste the URL you just copied in the Script URL field

#### User Setup
Once the project is setup
1. Open Borrit settings, Edit > Project Settings > Borrit
2. Select a username if it's empty
3. Make sure to input the appropriate Script URL if it's empty

## Usage example

### Settings
In the Project Settings window, you can find a new entry "Borrit", where you are able to set
your username (make it unique within your team!) and select the database use to store the state 
of your assets.

### Project
In the Project window, you can right click any assets or folder and select Borrit > Borrow.
This will mark the asset (or the folder and all its content) as borrowed by you. You can do the same
and select Return when you are done and the modified assets pushed to you remote repository.
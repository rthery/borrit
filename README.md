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
For the Google Sheets Database, you will need a bit of setup. Follow 
the instructions [here](https://developers.google.com/sheets/api/guides/authorizing) to create
an application that will allow you to access Google Sheets. Then download the OAuth 2.0 credentials
JSON file you'll obtained through Google Cloud Console.

Then place this file somewhere in your project (it needs to be shared with your team, so push it 
to your remote repository), a good place would be in `ProjectSettings\Packages\io.github.borrit`
beside the other settings of Borrit.

You should now be able to select that file in Edit > Project Settings > Borrit then create your 
authentication token (don't forget to set your username before!).
In addition to this, you will need to input the [spreadsheet id](https://developers.google.com/sheets/api/guides/concepts#spreadsheet_id) 
and make sure there's a sheet in it with the name of your project ([Application.productName](https://docs.unity3d.com/ScriptReference/Application-productName.html))

### Google App Script
If the project has already been set up you can skip this step and just input the script url.

For the Google App Script database, you will need to go to Google Drive, create a google spreadsheet, then create a Google App Script document and paste inside the content of this script: [GoogleAppScript.gs](Documentation/GoogleAppScripts/GoogleAppScript.gs) replace the #Your spreadsheet id goes here# test on the top of the script with your spreadsheet id(you can find on the url bar between the /d/ and the /edit parts), save the document. On the top right corner of the document there is a big blue button to deploy the script. Deploy it as a Web App. Copy the Web app URL that appears on the screen. 

In the Unity project, go to Edit > Project Settings > Borrit then paste the URL in the Script URL field.

## Usage example

### Settings
In the Project Settings window, you can find a new entry "Borrit", where you are able to set
your username (make it unique within your team!) and select the database use to store the state 
of your assets.

### Project
In the Project window, you can right click any assets or folder and select Borrit > Borrow.
This will mark the asset (or the folder and all its content) as borrowed by you. You can do the same
and select Return when you are done and the modified assets pushed to you remote repository.
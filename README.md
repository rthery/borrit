# Borrit

> Borrit is a tool for Unity adding a soft lock system for assets in the Editor

Working as a team on a Unity project can be troublesome because of potential difficult 
to resolve conflicts on some assets. This tool offers a simple way to borrow/return 
assets from the Project window directly.

It's currently independent from any version control system and try to support different
databases to store the state of the borrowed assets.

![Project window with Borrit](Documentation/images/example.png)

> :warning: **This is a Proof of Concept with support for Google Sheets only.**

## Installation
Use the package manager in Unity to install this package from its git url `https://github.com/rthery/borrit.git`

### Google Sheets (Google Script Web App)
This is the preferred method to use Google Sheets as a database, as its project setup is much easier 
and it drastically reduces the setup time for other developers (to nothing if they are using git)

#### Project Setup
1. Go to Google Drive and create a Spreadsheet  
1. Create a Google App Script document, Tools > Script editor  
1. Paste the content of [GoogleAppScript.gs](Documentation/GoogleAppScripts/GoogleAppScript.gs)  
1. Save the script  
1. Press Deploy > New Deploy (top right corner of the page)  
1. Select type 'Web App' and select 'Anyone' for 'Who has access'  
1. Confirm deploy and copy the Web App URL displayed  
1. In Unity, Open Borrit settings, Edit > Project Settings > Borrit  
1. Select 'Google App Script' Database  
1. Paste the URL you just copied in the Script URL field  
1. Then push to git (or your VCS of choice) the files in `ProjectSettings\Packages\io.github.borrit`  

#### User Setup
Once the project is setup, you should not need to do anything if you are using git. Otherwise verify
the following:
1. Open Borrit settings, Edit > Project Settings > Borrit  
1. Select a username if it's empty  
1. Paste the appropriate Script URL if it's empty  
1. You are ready to use Borrit  

### Google Sheets (Google API)
This method can be interesting if you want all your users to be authenticated with their Google account.
It makes it very cumbersome to set up for all your devs though.

#### Project Setup
1. Go to Google Drive and create a Spreadsheet with at least one sheet sheet in it with the name of your project
   ([Application.productName](https://docs.unity3d.com/ScriptReference/Application-productName.html))
1. Share as Editor with Google accounts who will use Borrit  
1. Follow the instructions [here](https://developers.google.com/sheets/api/guides/authorizing) to create
   an application that will allow you to access Google Sheets  
1. Download the OAuth 2.0 credentials JSON file you'll obtained through Google Cloud Console  
1. Place this file somewhere in your project, a good place would be in `ProjectSettings\Packages\io.github.borrit`
   beside the other settings of Borrit, if your repository is not public  
1. In Unity, Open Borrit settings, Edit > Project Settings > Borrit  
1. Select 'Google Sheets' Database  
1. Select the credentials.json file in the Credentials field  
1. Input the [spreadsheet id](https://developers.google.com/sheets/api/guides/concepts#spreadsheet_id)  

#### User Setup
Once the project is setup and credentials.json available somewhere 
1. Open Borrit settings, Edit > Project Settings > Borrit  
1. Select a username if it's empty  
1. Make sure Credentials file is present, otherwise select it  
1. Create your authentication token  
1. Connect and you are ready to use Borrit  

## Usage example

### Settings
In the Project Settings window, you can find a new entry "Borrit", where you are able to set
your username (make it unique within your team!) and select the database used to store the state 
of your assets.  
For the username, Borrit will by default use `git config user.name` if it's available

### Project
In the Project window, you can right click any assets or folder and select Borrit > Borrow.
This will mark the asset (or the folder and all its content) as borrowed by you. You can do the same
and select Return when you are done and the modified assets pushed to you remote repository.  
The state of assets if they borrowed or not are pulled in the interval specified in the settings.
You also have the option to Refresh manually via that context menu.
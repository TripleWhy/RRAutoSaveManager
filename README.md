# Rec Room Autosave Manager

Enhanced auto save management for [Rec Room](https://www.againstgrav.com/rec-room).

## Why?
Are you tired of
* having only one backup per room?
* having only 5 rooms backed up at all?
* having a limited restore history?

Then this tool is for you.

## What?
When you run AutosaveManager, it watches for autosave files that rec room uses and stores then whenever rec room updates them. It will show all of them in a list and let you restore any version you want.

## How?
### Installation
1. [Download](https://dotnet.microsoft.com/download) and install .NET Coreif you don't have it already. ([direct link](https://dotnet.microsoft.com/download/thank-you/dotnet-runtime-2.2.4-windows-hosting-bundle-installer) for windows)
2. Head over to [tags](https://gitlab.com/triplewhy/rrautosavemanager/tags) and download the latest binary.
3. Unpack the archive to a location of your choice.

### Usage
#### Running the Application
Start the application by double clicking AutoSaveManager.bat or running `dotnet AutoSaveManager.dll`.
It will watch for autosaves and store them as long as it's running.

Unfortunately, autosaves are only identified by a numeric ID. In order to keep track of your rooms you can name them and store notes for each snapshot.

#### Restoring Rooms
1. Select the room and snapshot you want to resstore and click the restore button.
2. In Rec Room, visit the room, if you are not already inside it.
3. Open your watch, open the room restore dialog, select the [Backup] line, and click ok.
Your room should now be in the desired state.

## Development
The project is not finished, what you see here is a preview. I might however not find the time to finish and polish it.
If you want to help, feel free submit a merge request or to contact me with any questions about the code.

# Rec Room Autosave Manager

Enhanced autosave management for [Rec Room](https://www.againstgrav.com/rec-room).

[[_TOC_]]

## Why?
Are you tired of
* having only one backup per room?
* having only 5 rooms backed up at all?
* having a limited restore history?

Then this tool is for you.

## What?
When you run AutosaveManager, it watches for autosave files that rec room uses and stores them whenever Rec Room updates them. It will show all of them in a list and let you restore any version you want.

## How?

### Download
| File                                                                                                | Notes                                                                                                                                                                                                                                                                                                                                                                                                                         |
|-----------------------------------------------------------------------------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| [Online Installer](http://triplewhy.gitlab.io/rrautosavemanager/RRAutosaveManagerSetup.exe)         | Will download selected components from the internet. Can be used to update your installation later.                                                                                                                                                                                                                                                                                                                           |
| [Offline Installer](http://triplewhy.gitlab.io/rrautosavemanager/RRAutosaveManagerSetupOffline.exe) | Contains all components. Can be used to update your installation later.                                                                                                                                                                                                                                                                                                                                                       |
| [Zip with dependencies](http://triplewhy.gitlab.io/rrautosavemanager/RRAutoSaveManager-w64-full.7z) | Contains Qt and .net core files. Requires Microsoft  [Visual C++ Redistributable](https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads), direct link  [here](https://aka.ms/vs/16/release/vc_redist.x64.exe).                                                                                                                                                                             |
| [Minimal Zip](http://triplewhy.gitlab.io/rrautosavemanager/RRAutoSaveManager-w64-minimal.7z)        | Does not contain Qt or .net core files. Will use a Qt binary shared with other Qml.net programs, or download one the first time it is started, if no binary is found. Global .net core installation required. Requires Microsoft  [Visual C++ Redistributable](https://support.microsoft.com/en-us/help/2977003/the-latest-supported-visual-c-downloads), direct link [here](https://aka.ms/vs/16/release/vc_redist.x64.exe). |

### Usage
#### Running the Application
Start the application by running `RRAutoSaveManager.exe`.
It will watch for autosaves and store them as long as it's running.

Unfortunately, autosaves are only identified by a numeric ID. In order to keep track of your rooms you can name them and store notes for each snapshot.

#### Restoring Rooms
1. Select the room and snapshot you want to resstore and click the restore button.
2. In Rec Room, visit the room, if you are not already inside it.
3. Open your watch, open the room restore dialog, select the [Backup] line, and click ok. Note that this has to be done before Rec Room overwrites the autosave with a new version.
Your room should now be in the desired state.

## State
The project is not finished, what you see here is a preview. I might however not find the time to finish and polish it.
If you want to help, feel free submit a merge request or to contact me with any questions about the code or planned features.

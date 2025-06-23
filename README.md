# NewYT - YouTube Video Tracker

A C# application that helps you track new videos from YouTube channels without having to use a Google account. The application consists of a web frontend for managing channels and a console application that automatically fetches new videos in the background.

## Features

- **Web Interface**: Web interface built with ASP.NET Core Razor Pages
- **Channel Management**: Add YouTube channels by their Channel ID
- **Video Tracking**: Automatically fetches new videos from RSS feeds
- **Mark as Watched**: Keep track of which videos you've already seen
- **Background Service**: Console application runs continuously to check for new videos
- **SQLite Database**: Lightweight database for storing channels and videos

## Architecture

The solution consists of three projects:

- **newyt.web**: ASP.NET Core web application with Razor Pages
- **newyt.console**: Console application with background service for fetching videos
- **newyt.shared**: Shared library containing data models, database context, and services

## Prerequisites

- .NET 9.0 SDK or later
- Any modern web browser

## Getting Started

### 1. Clone and Build

```bash
git clone https://github.com/btigi/newyt
cd newyt
dotnet build
```

### 2. Start the Applications

**Option A: Run both applications separately**

Terminal 1 - Start the web application:
```bash
cd newyt.web
dotnet run
```

Terminal 2 - Start the background video fetcher:
```bash
cd newyt.console
dotnet run
```

**Option B: Use the provided script**
```bash
start-applications.bat
```

### 3. Access the Web Interface

Open your browser and navigate to:
- HTTP: `http://localhost:5216`

## Usage

### Adding a YouTube Channel

1. Click the **"Add New Channel"** button on the main page
2. Enter a YouTube Channel ID (starts with "UC")
   - Example: `UCqk3CdGN_j8IR9z4uBbVPSg` (Lana Del Rey's channel)
3. Click **"Add Channel"**
4. NewYT will validate the channel and add it to your list

### Finding a Channel ID

You can find a YouTube channel ID in several ways:

1. **Channel URL**: If the URL is `youtube.com/channel/UCqk3CdGN_j8IR9z4uBbVPSg`, the ID is `UCqk3CdGN_j8IR9z4uBbVPSg`
2. **Custom URL**: If the channel has a custom URL, you'll need to view the page source and look for the channel ID

### Viewing and Managing Videos

- **Home Page**: Shows all unwatched videos from all your channels
- **Watch Video**: Click the "Watch" button to open the video in a new tab
- **Mark as Watched**: Click "Mark Watched" to remove the video from your unwatched list
- **Refresh Videos**: Click "Refresh Videos" to manually check for new content

### Background Video Fetching

The console application automatically:
- Checks for new videos every 15 minutes
- Downloads RSS feeds from all your channels
- Adds new videos to the database
- Logs all activity to the console

## Database

The application uses SQLite with the database file `newyt.db` created automatically in the application directory. The database contains:

- **Channels**: YouTube channels you're tracking
- **Videos**: Videos found from the RSS feeds with watch status

## Configuration

### Web Application (`newyt.web/appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=newyt.db"
  }
}
```

### Console Application (`newyt.console/appsettings.json`)
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=newyt.db"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

## Troubleshooting

### Common Issues

**"Channel not found" error**
- Verify the Channel ID is correct and starts with "UC"
- Check that the channel exists and has public videos
- Some channels may not have RSS feeds available

**Console app not finding new videos**
- Check the console output for error messages
- Verify internet connectivity
- YouTube RSS feeds may have rate limiting


## License

NewYT is licenced under the MIT license. Full licence details are available in license.md
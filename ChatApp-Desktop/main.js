const { app, BrowserWindow } = require('electron');
const path = require('path');
const isDev = require('electron-is-dev');
const { spawn } = require('child_process');
const fs = require('fs');

// Keep a reference of the window object to avoid it being garbage collected
let mainWindow;
let dotnetProcess;
const PORT = 5041;

function createWindow() {
  // Create the browser window
  mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: {
      nodeIntegration: false,
      contextIsolation: true
    },
    icon: path.join(__dirname, 'icon.ico')
  });

  // Start the ASP.NET Core process
  startAspNetCore();

  // Wait a moment for the server to start before loading the URL
  setTimeout(() => {
    // Load the localhost URL
    mainWindow.loadURL(`http://localhost:${PORT}`);
    
    // Open DevTools in dev mode
    // if (isDev) {
    //   mainWindow.webContents.openDevTools();
    // }
  }, 5000); // Increased timeout to 5 seconds to give more time to start

  mainWindow.on('closed', () => {
    mainWindow = null;
  });
}

function startAspNetCore() {
  // Path to the .NET executable
  const dotnetDllPath = path.join(__dirname, '..', 'ChatApp', 'src', 'ChatApp.Web', 'bin', 'Debug', 'net9.0', 'ChatApp.Web.dll');
  // Path to the project directory (where appsettings.json is located)
  const projectDir = path.join(__dirname, '..', 'ChatApp', 'src', 'ChatApp.Web');
  
  // Check if the path exists
  if (!fs.existsSync(dotnetDllPath)) {
    console.error(`ASP.NET Core application not found at ${dotnetDllPath}`);
    app.quit();
    return;
  }

  console.log(`Starting ASP.NET Core application from ${dotnetDllPath}`);
  console.log(`Working directory: ${projectDir}`);
  console.log(`Using port: ${PORT}`);
  
  // Start the ASP.NET Core process with the correct working directory
  // and environment variables to control the port
  dotnetProcess = spawn('dotnet', [dotnetDllPath], {
    cwd: projectDir,
    env: { 
      ...process.env,
      ASPNETCORE_ENVIRONMENT: "Development",
      ASPNETCORE_URLS: `http://localhost:${PORT}`
    }
  });

  // Log any output from the process
  dotnetProcess.stdout.on('data', (data) => {
    console.log(`ASP.NET Core: ${data}`);
  });

  dotnetProcess.stderr.on('data', (data) => {
    console.error(`ASP.NET Core error: ${data}`);
  });

  dotnetProcess.on('close', (code) => {
    console.log(`ASP.NET Core process exited with code ${code}`);
  });
}

app.on('ready', createWindow);

app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

app.on('activate', () => {
  if (mainWindow === null) {
    createWindow();
  }
});

app.on('quit', () => {
  // Kill the ASP.NET Core process when the Electron app quits
  if (dotnetProcess) {
    dotnetProcess.kill();
  }
});
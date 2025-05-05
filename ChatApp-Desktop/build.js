const { exec } = require('child_process');
const path = require('path');
const fs = require('fs');

// Path to the ASP.NET Core project
const aspNetCorePath = path.join(__dirname, '..', 'ChatApp', 'src', 'ChatApp.Web');

console.log('Building ASP.NET Core application...');
console.log(`Project path: ${aspNetCorePath}`);

// Check if the directory exists
if (!fs.existsSync(aspNetCorePath)) {
  console.error(`ASP.NET Core project not found at ${aspNetCorePath}`);
  process.exit(1);
}

// Execute dotnet build command
exec(`dotnet build "${aspNetCorePath}" --configuration Debug`, (error, stdout, stderr) => {
  if (error) {
    console.error(`Error building ASP.NET Core project: ${error.message}`);
    return;
  }
  
  if (stderr) {
    console.error(`Build stderr: ${stderr}`);
  }
  
  console.log(`Build stdout: ${stdout}`);
  console.log('Build completed successfully!');
  console.log('You can now run the desktop app with: npm start');
});
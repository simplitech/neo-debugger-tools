const {app, session, protocol, BrowserWindow, globalShortcut, Menu} = require('electron');
const path = require('path');
const proc = require('child_process');
const console = require('console');

app.console = new console.Console(process.stdout, process.stderr);

let mainWindow = null;
let serverProcess = null;

// Provide API for web application
global.callElectronUiApi = function(args) {
    console.log('Electron called from web app with args "' + args + '"');

    if (args) {
        if (args[0] === 'exit') {
            console.log('Kill server process');

            const kill = require('tree-kill');
            kill(serverProcess.pid, 'SIGTERM', function (err) {
                console.log('Server process killed');

                serverProcess = null;
                mainWindow.close();
            });
        }

        if (args[0] === 'minimize') {
            mainWindow.minimize();
        }

        if (args[0] === 'maximize') {
            if (!mainWindow.isMaximized()) {
                mainWindow.maximize();
            } else {
                mainWindow.unmaximize();
            }
        }
    }
};

// Quit when all windows are closed.
app.on('window-all-closed', () => {
	// On macOS it is common for applications and their menu bar
	// to stay active until the user quits explicitly with Cmd + Q
	if (process.platform !== 'darwin') {
	  app.quit()
	}
})

app.on('ready', function () {
    platform = process.platform;
    
	let serverPath = process.cwd() + '/bin/neod';
    if (platform === 'win32') {
		serverPath += ".exe";
    } 
    
	app.console.log("Spawning: "+serverPath);
    serverProcess = proc.spawn(serverPath);
    
    if (!serverProcess) {
        app.console.error('Unable to start server');
        app.quit();
        return;
    }

    serverProcess.stdout.on('data', function (data) {
        app.console.log('Server: ' + data);
    });

    app.console.log("Server PID: " + serverProcess.pid);

    let appUrl = 'http://localhost:7799';

	//On macOS the label of the application menu's first item is always your app's name, no matter what label you set. To change it, modify your app bundle's Info.plist file. See About Information Property List Files for more information.
	const createMenu = function() {
		 const template = [
			{
			  label: 'Edit',
			  submenu: [
				{role: 'undo'},
				{role: 'redo'},
				{type: 'separator'},
				{role: 'cut'},
				{role: 'copy'},
				{role: 'paste'},
				{role: 'pasteandmatchstyle'},
				{role: 'delete'},
				{role: 'selectall'}
			  ]
			},
			{
			  label: 'View',
			  submenu: [
				{role: 'reload'},
				{role: 'forcereload'},
				{role: 'toggledevtools'},
				{type: 'separator'},
				{role: 'resetzoom'},
				{role: 'zoomin'},
				{role: 'zoomout'},
				{type: 'separator'},
				{role: 'togglefullscreen'}
			  ]
			},
			{
			  label: 'Contract',
			  submenu: [
				{label: 'Run'},
				{label: 'Step'},
				{label: 'Reset'},
				{label: 'Rebuild'},
				{type: 'separator'},
				{label: 'Storage'},
			  ]
			},			
			{
			  role: 'window',
			  submenu: [
				{role: 'minimize'},
				{role: 'close'}
			  ]
			},
			{
			  role: 'help',
			  submenu: [
				{
				  label: 'Learn More',
				  click () { require('electron').shell.openExternal('https://electronjs.org') }
				}
			  ]
			}
		  ]
		  
		  if (process.platform === 'darwin') {
			template.unshift({
			  label: app.getName(),
			  submenu: [
				{role: 'about'},
				{type: 'separator'},
				{role: 'services', submenu: []},
				{type: 'separator'},
				{role: 'hide'},
				{role: 'hideothers'},
				{role: 'unhide'},
				{type: 'separator'},
				{role: 'quit'}
			  ]
			})
		  
			// Edit menu
			template[1].submenu.push(
			  {type: 'separator'},
			  {
				label: 'Speech',
				submenu: [
				  {role: 'startspeaking'},
				  {role: 'stopspeaking'}
				]
			  }
			)
		  
			// Window menu
			template[3].submenu = [
			  {role: 'close'},
			  {role: 'minimize'},
			  {role: 'zoom'},
			  {type: 'separator'},
			  {role: 'front'}
			]
		  }
		  
		  const menu = Menu.buildFromTemplate(template)
		  Menu.setApplicationMenu(menu)		
	}

    const createWindow = function () {
        mainWindow = new BrowserWindow({
            width: 800,
            height: 600,
			show: false
        });

		mainWindow.webContents.session.clearCache(function(){
			mainWindow.loadURL(appUrl);

			createMenu();
			
			mainWindow.webContents.on('did-finish-load', function() {
				mainWindow.show();
				mainWindow.setTitle('Neo Debugger');
			});	
			
			// uncomment to show debug tools
			// mainWindow.webContents.openDevTools();

			mainWindow.on('closed', function () {
				mainWindow = null;
			});

			/*mainWindow.on('close', function (e) {
				if (serverProcess) {
					e.preventDefault();

					mainWindow.webContents.executeJavaScript("appWindowExit();");
				}
			});*/
		
		});		
    };


	createWindow();

    // Register a shortcut listener.
    const ret = globalShortcut.register('CommandOrControl+Shift+`', () => {
        console.log('Bring to front shortcut triggered');
        if (mainWindow) {
            mainWindow.focus();
        }
    })
});

app.on('will-quit', () => {
    // Unregister all shortcuts.
    globalShortcut.unregisterAll();
});
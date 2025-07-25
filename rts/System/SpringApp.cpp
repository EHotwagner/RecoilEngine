/* This file is part of the Spring engine (GPL v2 or later), see LICENSE.html */

#include <functional>
#include <iostream>
#include <chrono>

#include <SDL.h>
#include <System/GflagsExt.h>

#ifdef _WIN32
//windows workarrounds
#undef KeyPress
#undef KeyRelease
#else
#include <unistd.h> // isatty
#include <X11/Xlib.h> // XInitThreads

#undef KeyPress
#undef KeyRelease
#undef GrayScale
#endif


#include "Rendering/GL/myGL.h"
#include "System/SpringApp.h"
#ifndef HEADLESS
#include "aGui/Gui.h"
#endif
#include "ExternalAI/AILibraryManager.h"
#include "Game/CameraHandler.h"
#include "Game/ClientSetup.h"
#include "Game/GameSetup.h"
#include "Game/GameVersion.h"
#include "Game/GameController.h"
#include "Game/Game.h"
#include "Game/GlobalUnsynced.h"
#include "Game/PreGame.h"
#include "Game/UI/KeyBindings.h"
#include "Game/UI/KeyCodes.h"
#include "Game/UI/ScanCodes.h"
#include "Game/UI/InfoConsole.h"
#include "Game/UI/MouseHandler.h"
#include "Lua/LuaOpenGL.h"
#include "Lua/LuaVFSDownload.h"
#include "Menu/LuaMenuController.h"
#include "Menu/SelectMenu.h"
#include "Net/GameServer.h"
#include "Net/Protocol/NetProtocol.h" // clientNet
#include "Rendering/GlobalRendering.h"
#include "Rendering/Fonts/glFont.h"
#include "Rendering/GL/FBO.h"
#include "Rendering/Models/ModelsMemStorage.h"
#include "Rendering/GL/RenderBuffers.h"
#include "Rendering/Shaders/ShaderHandler.h"
#include "Rendering/Textures/Bitmap.h"
#include "Rendering/Textures/NamedTextures.h"
#include "Rendering/Textures/TextureAtlas.h"
#include "Sim/Misc/DefinitionTag.h" // DefType
#include "Sim/Misc/GlobalSynced.h"
#include "Sim/Misc/ModInfo.h"
#include "Sim/Projectiles/ExplosionGenerator.h"
#include "System/ScopedResource.h"
#include "System/EventHandler.h"
#include "System/Exceptions.h"
#include "System/GlobalConfig.h"
#include "System/SpringMath.h"
#include "System/MsgStrings.h"
#include "System/SafeUtil.h"
#include "System/SplashScreen.hpp"
#include "System/SpringExitCode.h"
#include "System/StartScriptGen.h"
#include "System/TimeProfiler.h"
#include "System/UriParser.h"
#include "System/LoadLock.h"
#include "System/Config/ConfigHandler.h"
#include "System/creg/creg_runtime_tests.h"
#include "System/FileSystem/ArchiveScanner.h"
#include "System/FileSystem/DataDirLocater.h"
#include "System/FileSystem/DataDirsAccess.h"
#include "System/FileSystem/FileHandler.h"
#include "System/FileSystem/FileSystem.h"
#include "System/FileSystem/FileSystemInitializer.h"
#include "System/FileSystem/Misc.hpp"
#include "System/Input/KeyInput.h"
#include "System/Input/MouseInput.h"
#include "System/LoadSave/LoadSaveHandler.h"
#include "System/Log/ConsoleSink.h"
#include "System/Log/ILog.h"
#include "System/Log/DefaultFilter.h"
#include "System/LogOutput.h"
#include "System/Platform/errorhandler.h"
#include "System/Platform/CrashHandler.h"
#include "System/Platform/Threading.h"
#include "System/Platform/Watchdog.h"
#include "System/Sound/ISound.h"
#include "System/Sync/FPUCheck.h"
#include "System/Threading/ThreadPool.h"

#include "Game/UnsyncedGameCommands.h"
#include "Game/SyncedGameCommands.h"
#include "lib/luasocket/src/restrictions.h"



CONFIG(unsigned, SetCoreAffinity).defaultValue(0).safemodeValue(1).description("Defines a bitmask indicating which CPU cores the main-thread should use.");
CONFIG(unsigned, TextureMemPoolSize).defaultValue(512).minimumValue(0).description("Set to 0 to disable, otherwise specify a predefined memory to serve Bitmap allocation requests");
CONFIG(bool, UseLuaMemPools).defaultValue(true).description("Whether Lua VM memory allocations are made from pools.");
CONFIG(bool, UseHighResTimer).defaultValue(false).description("On Windows, sets whether Spring will use low- or high-resolution timer functions for tasks like graphical interpolation between game frames.");
CONFIG(bool, UseFontConfigLib).defaultValue(true).description("Whether the system fontconfig library (if present and enabled at compile-time) should be used for handling fonts.");
CONFIG(bool, UseFontConfigSystemFonts).defaultValue(true).description("Whether the system fonts will be searched by fontconfig.");
CONFIG(bool, FontConfigSearchAttributes).defaultValue(true).description("Whether the font characteristics will used to refine the search by fontconfig. Results in better glyph matches in some cases, but has a nontrivial performance cost.");
CONFIG(bool, FontConfigApplySubstitutions).defaultValue(true).description("[EXPERIMENTAL] In case it's disabled FcConfigSubstitute is not getting called, this might break non-ASCII font rendering.");
CONFIG(int, MaxFontTries).defaultValue(5).description("Represents the maximum number of attempts to search for a glyph replacement using the FontConfig library (lower = foreign glyphs may fail to render, higher = searching for foreign glyphs can lag the game).");
CONFIG(int, MaxPinnedFonts).defaultValue(10).description("Maximum number of fonts to pin to cache. Increasing this will eventually use more memory, but can alleviate processing spikes when rendering new glyphs.");

CONFIG(std::string, name).defaultValue(UnnamedPlayerName).description("Sets your name in the game. Since this is overridden by lobbies with your lobby username when playing, it usually only comes up when viewing replays or starting the engine directly for testing purposes.");
CONFIG(std::string, DefaultStartScript).defaultValue("").description("filename of script.txt to use when no command line parameters are specified.");
CONFIG(std::string, SplashScreenDir).defaultValue(".");



DEFINE_bool_EX  (sync_version,       "sync-version",       false, "Display program sync version (for online gaming)");
DEFINE_bool_EX  (gen_fontconfig,     "gen-fontconfig",     false, "Generate font-configuration database");
DEFINE_bool     (fullscreen,                               false, "Run in fullscreen mode");
DEFINE_bool     (window,                                   false, "Run in windowed mode");
DEFINE_bool     (hidden,                                   false, "Start in background (minimised, no taskbar entry)");
DEFINE_bool     (nocolor,                                  false, "Disables colorized stdout");
DEFINE_string   (server,                                   "",    "Set listening IP for server");
DEFINE_bool     (textureatlas,                             false, "Dump each finalized textureatlas in textureatlasN.tga");

DEFINE_bool_EX  (list_ai_interfaces,     "list-ai-interfaces",     false, "Dump a list of available AI Interfaces to stdout");
DEFINE_bool_EX  (list_skirmish_ais,      "list-skirmish-ais",      false, "Dump a list of available Skirmish AIs to stdout");
DEFINE_bool_EX  (list_config_vars,       "list-config-vars",       false, "Dump a list of config vars and meta data to stdout");
DEFINE_bool_EX  (list_def_tags,          "list-def-tags",          false, "Dump a list of all unitdef-, weapondef-, ... tags and meta data to stdout");
DEFINE_bool_EX  (list_unsynced_commands, "list-unsynced-commands", false, "Dump a list of all unsynced commands to stdout");
DEFINE_bool_EX  (list_synced_commands,   "list-synced-commands",   false, "Dump a list of all synced commands to stdout");
DEFINE_bool_EX  (list_ceg_classes,       "list-ceg-classes",       false, "Dump a list of available projectile classes to stdout");
DEFINE_bool_EX  (test_creg,              "test-creg",              false, "Test if all CREG classes are completed");

DEFINE_bool     (safemode,                                 false, "Turns off many things that are known to cause problems (i.e. on PC/Mac's with lower-end graphic cards)");

DEFINE_string   (config,                                   "",    "Exclusive configuration file");
DEFINE_bool     (isolation,                                false, "Limit the data-dir (games & maps) scanner to one directory");
DEFINE_string_EX(isolation_dir,      "isolation-dir",      "",    "Specify the isolation-mode data-dir (see --isolation)");
DEFINE_string_EX(write_dir,          "write-dir",          "",    "Specify where Spring writes to.");
DEFINE_string   (game,                                     "",    "Specify the game that will be instantly loaded");
DEFINE_string   (map,                                      "",    "Specify the map that will be instantly loaded");
DEFINE_string   (menu,                                     "",    "Specify a lua menu archive to be used by spring");
DEFINE_string   (name,                                     "",    "Set your player name");
DEFINE_bool     (oldmenu,                                  false, "Start the old menu");
DEFINE_string_EX(calc_checksum,      "calc-checksum",      "",    "Calculate named archive checksum and write to cache, cant run in parallel");

/* Startscript sets the listening port number. Replays use the entire startscript, including the port number.
 * So normally if two games were originally played on the same port number, you can't watch their replays in
 * parallel because they both try to open the same port. This makes automated replay parsing difficult when
 * the same port number is heavily reused across many replays. Forcing onlyLocal solves this. */
DEFINE_bool_EX  (onlyLocal,              "only-local",     false, "Force OnlyLocal mode (no network listening sockets). Use for parallelized watching of multiplayer replays");



int spring::exitCode = spring::EXIT_CODE_SUCCESS;

static unsigned int reloadCount = 0;
static unsigned int killedCount = 0;



// initialize basic systems for command line help / output
static void ConsolePrintInitialize(const std::string& configSource, bool safemode)
{
	spring_clock::PushTickRate(false);
	spring_time::setstarttime(spring_time::gettime(true));

	LOG_DISABLE();
	FileSystemInitializer::PreInitializeConfigHandler(configSource, "", safemode);
	FileSystemInitializer::InitializeLogOutput();
	LOG_ENABLE();
}

static void FlushExit()
{
	std::fflush(stdout);
}

/**
 * Initializes SpringApp variables
 *
 * @param argc argument count
 * @param argv array of argument strings
 */
SpringApp::SpringApp(int argc, char** argv)
{
	std::atexit(FlushExit);
	// NB
	//   {--,/}help overrides all other flags and causes exit(),
	//   even in the unusual event it is not given as first arg
	gflags::SetUsageMessage("Usage: " + std::string(argv[0]) + " [options] [path_to_script.txt or demo.sdfz]");
	gflags::SetVersionString(SpringVersion::GetFull());
	gflags::ParseCommandLineFlags(&argc, &argv, true);

	// also initializes configHandler and logOutput
	ParseCmdLine(argc, argv);

	spring_clock::PushTickRate(configHandler->GetBool("UseHighResTimer"));
	// set the Spring "epoch" to be whatever value the first
	// call to gettime() returns, should not be 0 (can safely
	// be done before SDL_Init, we are not using SDL_GetTicks
	// as our clock anymore)
	spring_time::setstarttime(spring_time::gettime(true));

	// gu does not exist yet, pre-seed for ShowSplashScreen
	guRNG.Seed(CGlobalUnsyncedRNG::rng_val_type(&argc));
	// ditto for unsynced Lua states (which do not use guRNG)
	spring_lua_unsynced_srand(nullptr);

	CLogOutput::LogSectionInfo();
	CLogOutput::LogConfigInfo();
	CLogOutput::LogSystemInfo(); // needs spring_clock
}

/**
 * Destroys SpringApp variables
 */
SpringApp::~SpringApp()
{
	spring_clock::PopTickRate();
}


/**
 * @brief Initializes the SpringApp instance
 * @return whether initialization was successful
 */
bool SpringApp::Init()
{
	SpringMath::Init();
	LuaMemPool::InitStatic(configHandler->GetBool("UseLuaMemPools"));

	CGlobalRendering::InitStatic();
	globalRendering->SetFullScreen(FLAGS_window, FLAGS_fullscreen);

	if (!InitPlatformLibs())
		return false;

	good_fpu_control_registers(__func__);

	// populate params
	globalConfig.Init();

	// Install Watchdog (must happen after time epoch is set)
	Watchdog::Install();
	Watchdog::RegisterThread(WDT_MAIN, true);

	// Create Window
	if (!InitWindow(("Recoil " + SpringVersion::GetFull()).c_str())) {
		SDL_Quit();
		return false;
	}

	Threading::SetThreadName("recoil-main"); // set default threadname for pstree

	// Init OpenGL
	globalRendering->PostInit();
	globalRendering->UpdateGLConfigs();
	globalRendering->UpdateGLGeometry();
	globalRendering->InitGLState();

	CCameraHandler::InitStatic();
	CBitmap::InitPool(configHandler->GetInt("TextureMemPoolSize"));

	UpdateInterfaceGeometry();
	InitFonts();

	ClearScreen();

	if (!InitFileSystem())
		return false;

	// Affinity
	Threading::SetThreadScheduler();

	CInfoConsole::InitStatic();
	CMouseHandler::InitStatic();

	inputToken = input.AddHandler([this](const SDL_Event& event) { return SpringApp::MainEventHandler(event); });

	// Global structures
	ENTER_SYNCED_CODE();
	gs->Init();
	LEAVE_SYNCED_CODE();
	gu->Init();

	// GUIs
	#ifndef HEADLESS
	agui::gui = new agui::Gui();
	#endif
	keyCodes.Reset();
	scanCodes.Reset();

	CNamedTextures::Init();
	LuaOpenGL::Init();
	ISound::Initialize(false);

	// Lua socket restrictions
	CLuaSocketRestrictions::InitStatic();
	LuaVFSDownload::Init();

	// Create CGameSetup and CPreGame objects
	Startup();
	return true;
}


bool SpringApp::InitPlatformLibs()
{
#if !(defined(_WIN32) || defined(__APPLE__) || defined(HEADLESS)) || defined(__OpenBSD__)
	// MUST run before any other X11 call (including
	// those by SDL) to make calls to X11 threadsafe
	if (!XInitThreads()) {
		LOG_L(L_FATAL, "[SpringApp::%s] Xlib is not threadsafe", __func__);
		return false;
	}
#endif

#if defined(_WIN32) && defined(__GNUC__)
	// load QTCreator's gdb helper dll; a variant of this should also work on other OSes
	{
		// suppress dialog box if gdb helpers aren't found
		const UINT oldErrorMode = SetErrorMode(SEM_FAILCRITICALERRORS);

		if (LoadLibrary("gdbmacros.dll"))
			LOG_L(L_DEBUG, "[SpringApp::%s] QTCreator's gdbmacros.dll loaded", __func__);

		SetErrorMode(oldErrorMode);
	}
#endif

	return true;
}

bool SpringApp::InitFonts()
{
	FtLibraryHandlerProxy::InitFtLibrary();
	FtLibraryHandlerProxy::InitFontconfig(false);
	CFontTexture::InitFonts();
	return CglFont::LoadConfigFonts();
/*
	using namespace std::chrono_literals;
	auto future = std::async(std::launch::async, []() {
		return FtLibraryHandlerProxy::CheckGenFontConfigFull(false);
	});

	for (;;) {
		auto status = future.wait_for(16.6666ms); //60 FPS
		if (status == std::future_status::ready) {
			if (future.get() == false)
				return false;

			return ret;
		}
		else {
			Watchdog::ClearTimer(WDT_MAIN);
			SDL_PumpEvents();
			globalRendering->SwapBuffers(true, true);
		}
	}
*/
}

void SpringApp::CleanFonts()
{
	font      = {};
	smallFont = {};

	// Can't leave it to default program destructor as the order of object destruction is not guaranteed.
	// E.g.: Bitmap memory pool can be destroyed before fonts causing null ptr exception
	CFontTexture::KillFonts();
}

bool SpringApp::InitFileSystem()
{
	bool ret = false;

	// ArchiveScanner uses for_mt, so must set a thread-count
	// (employ all available threads, then switch to default)
	ThreadPool::SetMaximumThreadCount();

	// threaded initialization s.t. the window gets CPU time
	// FileSystem is mostly self-contained, don't need locks
	// (at this point neither the platform CWD nor data-dirs
	// have been set yet by FSI, can only use absolute paths)

	spring::thread fsInitThread(FileSystemInitializer::InitializeThr, &ret);

	#ifndef HEADLESS
	const auto splashScreenFiles = FileSystemMisc::GetSplashScreenFiles();
	if (!splashScreenFiles.empty()) {
		ShowSplashScreen(splashScreenFiles[ guRNG.NextInt(splashScreenFiles.size()) ], SpringVersion::GetFull(), [&]() { return (FileSystemInitializer::Initialized()); });
	} else {
		ShowSplashScreen("", SpringVersion::GetFull(), [&]() { return (FileSystemInitializer::Initialized()); });
	}
	#endif

	fsInitThread.join();

	ThreadPool::SetDefaultThreadCount();
	// see InputHandler::PushEvents
	streflop::streflop_init<streflop::Simple>();
	return ret;
}

/**
 * @return whether window initialization succeeded
 * @param title char* string with window title
 *
 * Initializes the game window
 */
bool SpringApp::InitWindow(const char* title)
{
	// SDL will cause a creation of gpu-driver thread that will clone its name from the starting threads (= this one = mainthread)
	Threading::SetThreadName("gpu-driver");

	// raises an error-prompt in case of failure
	if (!globalRendering->CreateWindowAndContext(title))
		return false;

	// Something in SDL_SetVideoMode (OpenGL drivers?) messes with the FPU control word.
	// Set single precision floating point math.
	streflop::streflop_init<streflop::Simple>();

	// any other thread spawned from the main-process should be `unknown`
	Threading::SetThreadName("unknown");
	return true;
}


/**
 * Saves position of the window, if we are not in full-screen mode
 */
void SpringApp::SaveWindowPosAndSize()
{
	globalRendering->ReadWindowPosAndSize();
	globalRendering->SaveWindowPosAndSize();
}


void SpringApp::UpdateInterfaceGeometry()
{
	#ifndef HEADLESS
	const int vpx = globalRendering->viewPosX;
	const int vpy = globalRendering->viewWindowOffsetY;

	agui::gui->UpdateScreenGeometry(globalRendering->viewSizeX, globalRendering->viewSizeY, vpx, vpy);
	#endif
}


/**
 * @return whether commandline parsing was successful
 *
 * Parse command line arguments
 */
void SpringApp::ParseCmdLine(int argc, char* argv[])
{
	if (argc >= 2)
		inputFile = argv[1];

#ifndef _WIN32
	if (!FLAGS_nocolor && (getenv("SPRING_NOCOLOR") == nullptr)) {
		// don't colorize, if our output is piped to a diff tool or file
		if (isatty(fileno(stdout)))
			log_console_colorizedOutput(true);
	}
#endif

	if (FLAGS_isolation)
		dataDirLocater.SetIsolationMode(true);

	if (!FLAGS_isolation_dir.empty()) {
		dataDirLocater.SetIsolationMode(true);
		dataDirLocater.SetIsolationModeDir(FLAGS_isolation_dir);
	}

	if (!FLAGS_write_dir.empty())
		dataDirLocater.SetWriteDir(FLAGS_write_dir);

	if (FLAGS_gen_fontconfig) {
		{
			spring_clock::PushTickRate();
			spring_time::setstarttime(spring_time::gettime(true));
		}
		FtLibraryHandlerProxy::InitFtLibrary();
		if (FtLibraryHandlerProxy::InitFontconfig(true)) {
			printf("[FtLibraryHandler::GenFontConfig] is succesfull\n");
			exit(spring::EXIT_CODE_SUCCESS);
		}
		else {
			printf("[FtLibraryHandler::GenFontConfig] is unsuccesfull\n");
			exit(spring::EXIT_CODE_FAILURE);
		}
	}

	if (FLAGS_sync_version) {
		// Note, the missing "Spring " is intentionally to make it compatible with `spring-dedicated --sync-version`
		std::cout << SpringVersion::GetSync() << std::endl;
		exit(spring::EXIT_CODE_SUCCESS);
	}

	// Interface Documentations in JSON-Format
	if (FLAGS_list_config_vars) {
		ConfigVariable::OutputMetaDataMap();
		exit(spring::EXIT_CODE_SUCCESS);
	}
	if (FLAGS_list_def_tags) {
		DefType::OutputTagMap();
		exit(spring::EXIT_CODE_SUCCESS);
	}
	if (FLAGS_list_unsynced_commands) {
		UnsyncedGameCommands::CreateInstance();
		unsyncedGameCommands->AddDefaultActionExecutors();
		std::cout << unsyncedGameCommands->JsonOutput();
		exit(spring::EXIT_CODE_SUCCESS);
	}
	if (FLAGS_list_synced_commands) {
		SyncedGameCommands::CreateInstance();
		syncedGameCommands->AddDefaultActionExecutors();
		std::cout << syncedGameCommands->JsonOutput();
		exit(spring::EXIT_CODE_SUCCESS);
	}
	if (FLAGS_list_ceg_classes)
		exit(CCustomExplosionGenerator::OutputProjectileClassInfo() ? spring::EXIT_CODE_SUCCESS : spring::EXIT_CODE_FAILURE);

	// Runtime Tests
	if (FLAGS_test_creg) {
#ifdef USING_CREG
		exit(creg::RuntimeTest() ? spring::EXIT_CODE_SUCCESS : spring::EXIT_CODE_FAILURE);
#else
		exit(spring::EXIT_CODE_SUCCESS);
#endif
	}

	// mutually exclusive options that cause spring to quit immediately
	if (FLAGS_list_ai_interfaces) {
		ConsolePrintInitialize(FLAGS_config, FLAGS_safemode);
		AILibraryManager::OutputAIInterfacesInfo();
		exit(spring::EXIT_CODE_SUCCESS);
	}
	else if (FLAGS_list_skirmish_ais) {
		ConsolePrintInitialize(FLAGS_config, FLAGS_safemode);
		AILibraryManager::OutputSkirmishAIInfo();
		exit(spring::EXIT_CODE_SUCCESS);
	}
	else if (!FLAGS_calc_checksum.empty()) {
		ConsolePrintInitialize(FLAGS_config, FLAGS_safemode);
		try {
			FileSystemInitializer::InitializeTry();
			archiveScanner->ResetNumFilesHashed();

			const std::string archive = archiveScanner->ArchiveFromName(FLAGS_calc_checksum);
			const auto cs = archiveScanner->GetArchiveCompleteChecksumBytes(archive);

			sha512::hex_digest hexCs = { 0 };
			sha512::dump_digest(cs, hexCs);

			LOG("Archive \"%s\", checksum = \"%s\"", FLAGS_calc_checksum.c_str(), hexCs.data());
			FileSystemInitializer::Cleanup();
			exit(spring::EXIT_CODE_SUCCESS);
		}
		CATCH_SPRING_ERRORS
		exit(spring::EXIT_CODE_CRASHED);
	}

	CTextureAtlas::SetDebug(FLAGS_textureatlas);

	CGameSetup::forceOnlyLocal = FLAGS_onlyLocal;

	// if this fails, configHandler remains null
	// logOutput's init depends on configHandler
	FileSystemInitializer::PreInitializeConfigHandler(FLAGS_config, FLAGS_name, FLAGS_safemode);
	FileSystemInitializer::InitializeLogOutput();
}


CGameController* SpringApp::LoadSaveFile(const std::string& saveFile)
{
	clientSetup->isHost = true;

	pregame = new CPreGame(clientSetup);
	pregame->AsyncExecute(&CPreGame::LoadSaveFile, saveFile);
	//pregame->LoadSaveFile(saveFile);
	return pregame;
}


CGameController* SpringApp::LoadDemoFile(const std::string& demoFile)
{
	clientSetup->isHost = true;
	clientSetup->myPlayerName += " (spec)";

	pregame = new CPreGame(clientSetup);
	pregame->AsyncExecute(&CPreGame::LoadDemoFile, demoFile);
	//pregame->LoadDemoFile(demoFile);
	return pregame;
}


CGameController* SpringApp::RunScript(const std::string& buf)
{
	try {
		clientSetup.reset(new ClientSetup());
		clientSetup->LoadFromStartScript(buf);
	} catch (const content_error& err) {
		throw content_error(std::string("Invalid script file\n") + err.what());
	}

	if (!clientSetup->demoFile.empty())
		return LoadDemoFile(clientSetup->demoFile);

	if (!clientSetup->saveFile.empty())
		return LoadSaveFile(clientSetup->saveFile);

	// LoadFromStartScript overrides all values so reset cmdline defined ones
	if (!FLAGS_server.empty()) {
		clientSetup->hostIP = FLAGS_server;
		clientSetup->isHost = true;
	}

	clientSetup->SanityCheck();

	pregame = new CPreGame(clientSetup);

	if (clientSetup->isHost) {
		pregame->AsyncExecute(&CPreGame::LoadSetupScript, buf);
		//pregame->LoadSetupScript(buf);
	}

	return pregame;
}

void SpringApp::StartScript(const std::string& script)
{
	// startscript
	LOG("[%s] Loading StartScript from: %s", __func__, script.c_str());
	CFileHandler fh(script, SPRING_VFS_PWD_ALL);
	if (!fh.FileExists())
		throw content_error("Setup-script does not exist in given location: " + script);

	std::string buf;
	if (!fh.LoadStringData(buf))
		throw content_error("Setup-script cannot be read: " + script);

	activeController = RunScript(buf);
}

void SpringApp::LoadSpringMenu()
{
	const std::string  vfsScript = "defaultstartscript.txt";
	const std::string& cfgScript = configHandler->GetString("DefaultStartScript");

	const std::string& startScript = (cfgScript.empty() && CFileHandler::FileExists(vfsScript, SPRING_VFS_PWD_ALL))? vfsScript: cfgScript;

	// bypass default menu if we have a valid LuaMenu handler
	if (CLuaMenuController::ActivateInstance(""))
		return;

	if (FLAGS_oldmenu || startScript.empty()) {
		// old menu
	#ifdef HEADLESS
		handleerror(nullptr,
			"The headless version of the engine can not be run in interactive mode.\n"
			"Please supply a start-script, save- or demo-file.", "ERROR", MBF_OK | MBF_EXCL);
	#endif
		// not a memory-leak: SelectMenu deletes itself on start
		activeController = new SelectMenu(clientSetup);
	} else {
		// run custom menu from game and map
		StartScript(startScript);
	}
}

/**
 * Initializes instance of GameSetup
 */
void SpringApp::Startup()
{
	// bash input
	const std::string& extension = FileSystem::GetExtension(inputFile);

	// note: avoid any .get() leaks between here and GameServer!
	clientSetup.reset(new ClientSetup());

	// create base client-setup
	if (!FLAGS_server.empty()) {
		clientSetup->hostIP = FLAGS_server;
		clientSetup->isHost = true;
	}

	clientSetup->myPlayerName = configHandler->GetString("name");
	clientSetup->SanityCheck();

	luaMenuController = new CLuaMenuController(FLAGS_menu);

	// no argument (either game is given or show selectmenu)
	if (inputFile.empty()) {
		clientSetup->isHost = true;

		if ((!FLAGS_game.empty()) && (!FLAGS_map.empty())) {
			// --game and --map directly specified, try to run them
			activeController = RunScript(StartScriptGen::CreateMinimalSetup(FLAGS_game, FLAGS_map));
			return;
		}

		LoadSpringMenu();
		return;
	}

	// process given argument
	if (inputFile.find("spring://") == 0) {
		// url (syntax: spring://username:password@host:port)
		if (!ParseSpringUri(inputFile, clientSetup->myPlayerName, clientSetup->myPasswd, clientSetup->hostIP, clientSetup->hostPort))
			throw content_error("invalid url specified: " + inputFile);

		clientSetup->isHost = false;
		pregame = new CPreGame(clientSetup);
		return;
	}
	if (extension == "sdfz") {
		LoadDemoFile(inputFile);
		return;
	}
	if (extension == "slsf" || extension == "ssf") {
		LoadSaveFile(inputFile);
		return;
	}

	StartScript(inputFile);
}



void SpringApp::Reload(const std::string script)
{
	LOG("[SpringApp::%s][1]", __func__);

	// get rid of any running worker threads
	ThreadPool::SetThreadCount(0);
	ThreadPool::SetDefaultThreadCount();

	LOG("[SpringApp::%s][2]", __func__);

	if (gameServer != nullptr)
		gameServer->SetReloading(true);

	if (clientNet != nullptr)
		clientNet->ResetDemoRecorder();

	// Lua shutdown functions need to access 'game' but spring::SafeDelete sets it to NULL.
	// ~CGame also calls this, which does not matter because Lua handlers are gone by then
	if (game != nullptr)
		game->KillLua(false);

	LOG("[SpringApp::%s][3]", __func__);

	// kill sound here; thread might access readMap which is deleted by ~CGame
	ISound::Shutdown(true);

	LOG("[SpringApp::%s][4]", __func__);

	// PreGame allocates clientNet, so we need to delete our old connection
	spring::SafeDelete(game);
	spring::SafeDelete(pregame);

	spring::SafeDelete(clientNet);
	// no-op if we are not the server
	spring::SafeDelete(gameServer);

	LOG("[SpringApp::%s][5]", __func__);

	// do not stop running downloads when reloading
	LuaVFSDownload::Free(false);

	LOG("[SpringApp::%s][6]", __func__);

	#if 0
	// note: technically we only need to use RemoveArchive
	FileSystemInitializer::Cleanup(false);
	FileSystemInitializer::Initialize();
	#else
	// do not cleanup+reinit; LuaVFS thread might see NULL while scanner is temporarily gone
	// handling that in ScanAllDirs would leave the archive-cache incomplete, which also has
	// implications for sync
	FileSystemInitializer::Reload();
	#endif

	LOG("[SpringApp::%s][7]", __func__);

	CNamedTextures::Kill();
	CNamedTextures::Init();

	LuaOpenGL::Free();
	LuaOpenGL::Init();

	/*
	* // Some bitmaps are still allocated even after CglFont::ReallocSystemFontAtlases(true);
	* // E.g. WindowManagerHelper keeps a global bitmap
	* // Too much effort for too little reward
	* // Therefore can't change pool size / type
	*
	CglFont::ReallocSystemFontAtlases(true);
	CBitmap::KillPool();
	CBitmap::InitPool(configHandler->GetInt("TextureMemPoolSize"));
	CglFont::ReallocSystemFontAtlases(false);
	*/

	LOG("[SpringApp::%s][8]", __func__);

	// reload sounds.lua in case we switched to a different game
	ISound::Initialize(true);

	// make sure all old EventClients are really gone (safety)
	eventHandler.ResetState();

	LuaVFSDownload::Init();

	LOG("[SpringApp::%s][10]", __func__);

	transformsMemStorage.Reset();
	gu->ResetState();

	ENTER_SYNCED_CODE();
	gs->ResetState();
	LEAVE_SYNCED_CODE();

	// will be reconstructed from given script
	gameSetup->ResetState();

	CTimeProfiler::GetInstance().ResetState();
	modInfo.ResetState();

	LOG("[SpringApp::%s][11]", __func__);

	// must hold or we would loop forever
	assert(!gu->globalReload);

	luaMenuController->Reset();
	// clean changed configs
	configHandler->Update();

	LOG("[SpringApp::%s][12] #script=" _STPF_ "", __func__, script.size());

	if (script.empty()) {
		// if no script, drop back to menu
		LoadSpringMenu();
	} else {
		activeController = RunScript(script);
	}

	LOG("[SpringApp::%s][13] reloadCount=%u\n\n\n", __func__, ++reloadCount);
}

/**
 * @return return code of ActiveController::Update
 */
bool SpringApp::Update()
{
	bool retc = true;
	bool swap = true;

	configHandler->Update();
	globalRendering->UpdateWindow();
	globalRendering->UpdateTimer();

	#if 0
	if (activeController == nullptr)
		return true;
	if (!activeController->Update())
		return false;
	if (!activeController->Draw())
		return true;
	#else
	// sic; Update can set the controller to null
	retc = (        activeController == nullptr || activeController->Update());

	auto lock = CLoadLock::GetUniqueLock();
	swap = (retc && activeController != nullptr && activeController->Draw());
	#endif

	// always swap by default, not doing so can upset some drivers
	globalRendering->SwapBuffers(swap, false);
	return retc;
}


/**
 * Executes the application
 * (contains main game loop)
 */
int SpringApp::Run()
{
	// always lives at the same address
	const Threading::Error* threadError = Threading::GetThreadErrorC();

	// initialize crash reporting
	CrashHandler::Install();

	// note: exceptions thrown by other threads are *not* caught here
	// ErrorMsgBox sets threadError if called from any non-main thread
	try {
		if ((gu->globalQuit = !Init() || gu->globalQuit))
			spring::exitCode = spring::EXIT_CODE_NOINIT;

		while (!gu->globalQuit) {
			Watchdog::ClearTimer(WDT_MAIN);
			input.PushEvents();

			// move to clear global data if a save is queued
			ILoadSaveHandler::CreateSave(std::move(globalSaveFileData));

			if (gu->globalReload) {
				// copy; reloadScript is cleared by ResetState
				Reload(gameSetup->reloadScript);
			} else {
				gu->globalQuit = (!Update() || gu->globalQuit);
			}
		}
	} CATCH_SPRING_ERRORS

	// no exception from main, check if some other thread interrupted our regular loop
	// in case one did, ErrorMessageBox will call ::Kill and forcibly exit the process
	if (!threadError->Empty()) {
		Threading::Error tempError;

		strncat(tempError.caption,    threadError->caption, sizeof(tempError.caption) - 1);
		strncat(tempError.message, "[thread::error::run] ", sizeof(tempError.message) - 1);
		strncat(tempError.message,    threadError->message, sizeof(tempError.message) - 1);

		ErrorMessageBox(tempError.message, tempError.caption, threadError->flags);
	}

	try {
		Kill(true);
	} CATCH_SPRING_ERRORS

	// no exception from main, but a thread might have thrown *during* ::Kill
	// do not attempt to call Kill a second time, just show the error message
	if (!threadError->Empty())
		LOG_L(L_ERROR, "[SpringApp::%s] errorMsg=\"[thread::error::kill] %s\" msgCaption=\"%s\"", __func__, threadError->message, threadError->caption);

	// cleanup signal handlers, etc
	CrashHandler::Remove();

	return spring::exitCode;
}



int SpringApp::PostKill(Threading::Error&& e)
{
	if (e.Empty())
		return (Watchdog::DeregisterCurrentThread());

	if (Threading::IsMainThread())
		return -1;

	// checked by Run() after Init()
	*(Threading::GetThreadErrorM()) = std::move(e);

	// gu always exists, though thread might be too late to interrupt Run
	return (gu->globalQuit = true);
}

/**
 * Deallocates and shuts down engine
 */
void SpringApp::Kill(bool fromRun)
{
	assert(Threading::IsMainThread());

	if (killedCount > 0) {
		assert(!fromRun);
		return;
	}
	if (!fromRun)
		Watchdog::ClearTimer();

	// block any (main-thread) exceptions thrown here from causing another Kill
	killedCount += 1;

	LOG("[SpringApp::%s][1] fromRun=%d", __func__, fromRun);
	ThreadPool::SetThreadCount(0);
	LOG("[SpringApp::%s][2]", __func__);
	LuaVFSDownload::Free(true);

	// save window state early for the same reason as client demo
	if (globalRendering != nullptr)
		SaveWindowPosAndSize();
	// see ::Reload
	if (game != nullptr)
		game->KillLua(false);
	// write the demo before destroying game, such that it can not
	// be affected by a crash in any of the Game::Kill* functions
	if (clientNet != nullptr)
		clientNet->ResetDemoRecorder();

	// see ::Reload
	ISound::Shutdown(false);

	spring::SafeDelete(game);
	spring::SafeDelete(pregame);
	spring::SafeDelete(luaMenuController);

	LuaMemPool::KillStatic();

	LOG("[SpringApp::%s][3]", __func__);
	spring::SafeDelete(clientNet);
	spring::SafeDelete(gameServer);

	LOG("[SpringApp::%s][4] font=%p", __func__, font.get());
	#ifndef HEADLESS
	spring::SafeDelete(agui::gui);
	#endif

	CleanFonts();


	LOG("[SpringApp::%s][5]", __func__);
	CNamedTextures::Kill(true);

	CCameraHandler::KillStatic();

	CInfoConsole::KillStatic();
	CMouseHandler::KillStatic();

	LOG("[SpringApp::%s][6]", __func__);
	gs->Kill();
	gu->Kill();

	LOG("[SpringApp::%s][7]", __func__);

	CGlobalRendering::KillStatic();
	CBitmap::KillPool();
	CLuaSocketRestrictions::KillStatic();

	// also gets rid of configHandler
	FileSystemInitializer::Cleanup();
	DataDirLocater::FreeInstance();
	ThreadPool::ClearExtJobs();

	LOG("[SpringApp::%s][8]", __func__);
	Watchdog::DeregisterThread(WDT_MAIN);
	Watchdog::Uninstall();
	LOG("[SpringApp::%s][9]", __func__);

	killedCount -= 1;
}


bool SpringApp::MainEventHandler(const SDL_Event& event)
{
	switch (event.type) {
		case SDL_WINDOWEVENT: {
			switch (event.window.event) {
				case SDL_WINDOWEVENT_MOVED: {
					LOG("[SpringApp::%s][SDL_WINDOWEVENT_MOVED][1] di=%d, ssx=%d, ssy=%d, wsx=%d, wsy=%d, wpx=%d, wpy=%d"
						, __func__
						, globalRendering->GetCurrentDisplayIndex()
						, globalRendering->screenSizeX
						, globalRendering->screenSizeY
						, globalRendering->winSizeX
						, globalRendering->winSizeY
						, globalRendering->winPosX
						, globalRendering->winPosY);

					SaveWindowPosAndSize();

					if (globalRendering->numDisplays > 1 && globalRendering->dualScreenMode) {
						{
							SCOPED_ONCE_TIMER("GlobalRendering::UpdateGL");

							globalRendering->UpdateGLConfigs();
							globalRendering->UpdateGLGeometry();
							globalRendering->InitGLState();
							UpdateInterfaceGeometry();
						}
					}

					LOG("[SpringApp::%s][SDL_WINDOWEVENT_MOVED][2] di=%d, ssx=%d, ssy=%d, wsx=%d, wsy=%d, wpx=%d, wpy=%d"
						, __func__
						, globalRendering->GetCurrentDisplayIndex()
						, globalRendering->screenSizeX
						, globalRendering->screenSizeY
						, globalRendering->winSizeX
						, globalRendering->winSizeY
						, globalRendering->winPosX
						, globalRendering->winPosY);
				} break;
				// case SDL_WINDOWEVENT_RESIZED: // always preceded by CHANGED
				case SDL_WINDOWEVENT_SIZE_CHANGED: {
					LOG("[SpringApp::%s][SDL_WINDOWEVENT_SIZE_CHANGED][1] fullScreen=%d", __func__, globalRendering->fullScreen);

					Watchdog::ClearTimer(WDT_MAIN, true);

					{
						SCOPED_ONCE_TIMER("GlobalRendering::UpdateGL");

						SaveWindowPosAndSize();
						globalRendering->UpdateGLConfigs();
						globalRendering->UpdateGLGeometry();
						globalRendering->InitGLState();
						UpdateInterfaceGeometry();
					}
					{
						SCOPED_ONCE_TIMER("ActiveController::ResizeEvent");

						activeController->ResizeEvent();
						mouseInput->InstallWndCallback();
					}

					LOG("[SpringApp::%s][SDL_WINDOWEVENT_SIZE_CHANGED][2]\n", __func__);
				} break;
				case SDL_WINDOWEVENT_MAXIMIZED:
				case SDL_WINDOWEVENT_RESTORED:
				case SDL_WINDOWEVENT_SHOWN: {
					LOG("%s", "");
					LOG("[SpringApp::%s][SDL_WINDOWEVENT_SHOWN][1] fullScreen=%d", __func__, globalRendering->fullScreen);

					// reactivate sounds and other
					globalRendering->active = true;

					if (ISound::IsInitialized()) {
						SCOPED_ONCE_TIMER("Sound::Iconified");
						sound->Iconified(false);
					}

					if (globalRendering->fullScreen) {
						SCOPED_ONCE_TIMER("FBO::GLContextReinit");
						FBO::GLContextReinit();
					}

					LOG("[SpringApp::%s][SDL_WINDOWEVENT_SHOWN][2]\n", __func__);
				} break;
				case SDL_WINDOWEVENT_MINIMIZED:
				case SDL_WINDOWEVENT_HIDDEN: {
					LOG("%s", "");
					LOG("[SpringApp::%s][SDL_WINDOWEVENT_HIDDEN][1] fullScreen=%d", __func__, globalRendering->fullScreen);

					// deactivate sounds and other
					globalRendering->active = false;

					if (ISound::IsInitialized()) {
						SCOPED_ONCE_TIMER("Sound::Iconified");
						sound->Iconified(true);
					}

					if (globalRendering->fullScreen) {
						SCOPED_ONCE_TIMER("FBO::GLContextLost");
						FBO::GLContextLost();
					}

					LOG("[SpringApp::%s][SDL_WINDOWEVENT_HIDDEN][2]\n", __func__);
				} break;

				case SDL_WINDOWEVENT_FOCUS_GAINED: {
					// update keydown table
					KeyInput::Update(keyBindings.GetFakeMetaKey());
				} break;
				case SDL_WINDOWEVENT_FOCUS_LOST: {
					Watchdog::ClearTimer(WDT_MAIN, true);

					// SDL has some bug and does not update modstate on alt+tab/minimize etc.
					//FIXME check if still happens with SDL2 (2013)
					SDL_SetModState((SDL_Keymod)(SDL_GetModState() & (KMOD_NUM | KMOD_CAPS | KMOD_MODE)));

					// release all keyboard keys
					KeyInput::ReleaseAllKeys();

					if (mouse != nullptr) {
						// simulate mouse release to prevent hung buttons
						for (int i = 1; i <= NUM_BUTTONS; ++i) {
							if (!mouse->buttons[i].pressed)
								continue;

							SDL_Event event;
							event.type = event.button.type = SDL_MOUSEBUTTONUP;
							event.button.state = SDL_RELEASED;
							event.button.which = 0;
							event.button.button = i;
							event.button.x = -1;
							event.button.y = -1;
							SDL_PushEvent(&event);
						}

						// unlock mouse
						if (mouse->locked)
							mouse->ToggleMiddleClickScroll();
					}

					// and make sure to un-capture mouse
					globalRendering->SetWindowInputGrabbing(false);
				} break;

				case SDL_WINDOWEVENT_CLOSE: {
					gu->globalQuit = true;
				} break;
			};
		} break;
		case SDL_AUDIODEVICEREMOVED: {
			LOG("[SpringApp::%s][SDL_AUDIODEVICEREMOVED][1] type=%u, which=%u, iscapture=%u", __func__, event.adevice.type, event.adevice.which, static_cast<uint32_t>(event.adevice.iscapture));
			sound->DeviceChanged(event.adevice.which);
		} break;
		case SDL_QUIT: {
			gu->globalQuit = true;
		} break;
		case SDL_TEXTEDITING: {
			if (activeController != nullptr)
				activeController->TextEditing(event.edit.text, event.edit.start, event.edit.length);

		} break;
		case SDL_TEXTINPUT: {
			if (activeController != nullptr)
				activeController->TextInput(event.text.text);

		} break;
		case SDL_KEYDOWN: {
			KeyInput::Update(keyBindings.GetFakeMetaKey());

			if (activeController != nullptr) {
				int keyCode = CKeyCodes::GetNormalizedSymbol(event.key.keysym.sym);
				int scanCode = CScanCodes::GetNormalizedSymbol(event.key.keysym.scancode);
				activeController->KeyPressed(keyCode, scanCode, event.key.repeat);
			}

		} break;
		case SDL_KEYUP: {
			KeyInput::Update(keyBindings.GetFakeMetaKey());

			if (activeController != nullptr) {
				gameTextInput.ignoreNextChar = false;
				int keyCode = CKeyCodes::GetNormalizedSymbol(event.key.keysym.sym);
				int scanCode = CScanCodes::GetNormalizedSymbol(event.key.keysym.scancode);
				activeController->KeyReleased(keyCode, scanCode);
			}
		} break;
		case SDL_KEYMAPCHANGED: {
			if (activeController != nullptr) {
				activeController->KeyMapChanged();
			}

		} break;
	};

	return false;
}

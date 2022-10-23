# ObsidianCore Launcher

### Info

This launcher is designed to download and launch a single predefined version of Minecraft. While it can launch multiple versions from the same location by changing the config file this is not it's intended purpose. The purpose of this launcher is to be able to send something simple to friends when playing together that will set up the game for them.

The launcher supports the Microsoft account login and only the Microsoft account login. The legacy Minecraft and Mojang logins were being phased out as this was being developed and I saw no valid reasons to spend time implementing them. Nor did I have a way to test them as my account and that of everyone I knew were already migrated.

The launcher is compatible with Windows, Linux and Mac. It runs on Windows and Linux with .NET 6 and on Mac with Xamarin.Mac.

The Core of the launcher is a .NET Standard 2.0 library. The WPF Launcher and GTK launchers are .NET 6 while the Cocoa launcher runs on Mono.

### Compiling
The WPF version can only be compiled on Windows and the Cocoa version only on a Mac.
The GTK version can be compiled on any platform but making it work on anything but Linux will require additional work.

Make sure to restore the NuGet packages.

The WPF and GTK versions can be compiled with the command line `dotnet` tool or Visual Studio. The Cocoa version can be compiled with Visual Studio for Mac.

There is also a Java part of the launcher. It can be compiled with IntelliJ IDEA on any platform on which it runs.

### Running and building

For tests the running directory should be set to the `shared` directory.

For Cocoa the precompiler modifies the path to search for the game files in the `shared` directory while debugging and in the directory where the .app is located when released. I realize this is not how Mac apps usually work, but it's in line with where the WPF and GTK versions search for files.

For building, first load the Java project and run "Rebuild project". This will download the required libraries, compile the files and output them into the `shared` directory.

The WPF and GTK projects can be published with the `dotnet` tool or Visual Studio. Copy the contents of `shared` to where you published the application to provide the stuff the launcher needs to run the game.

#### Running the game

The launcher requires a `game.xml` file to be present in the same directory as the launcher is being run from (during developement that would be the `shared` directory) to tell it what to launch:

The basic structure of the file is as follows:

```
<?xml version="1.0" encoding="utf-8"?>
<game>
  <name>Name to display in the launcher</name>
  
  <!-- Optional but recommended: Specifies a file that guarantees two launchers/games can't be run at the same time. -->
  <lock-file>game.lock</lock-file>
  
  <!-- Optional: Path to the Java executable.
  If this is specified and the "Java executable" GUI setting is empty this will be used.
  ${os} is replaced with "windows", "linux" or "osx".
  ${arch} is replaced with "x86", "x64", "arm" or "aarch64".
  You don't need to add .exe for Windows. -->
  <java-bin>../${os}-${arch}-jre8/bin/java</java-bin>
  
  <!-- If this is true assets will be shared with the Vanilla installation, but won't be downloaded if they aren't already present. This is usefull when developing. -->
  <share-assets>true</share-assets>
  
  <!--One (and only one) of the following tags is required: -->
  
  <!-- Launcher the specified Vanilla version. -->
  <launch-version>...</launch-version>
  
  <!-- Launcher the specified Forge version. -->
  <launch-forge>...</launch-forge>
  
  <!-- Launcher the specified Fabric version. -->
  <launch-fabric>...</launch-fabric>
  
  <!-- Manually specify the arguments to pass to the game. Usefull for debugging. -->
  <launch-args>
    <vm-args><![CDATA[
One argument
per line is acceptable
    ]]></vm-args>
    <main-class>full name of main class</main-class>
    <game-args><![CDATA[
to make things more easily readable.
    ]]></game-args>
  </launch-args>
  
  <!-- When using <launch-args> you can tell the launcher to fetch the assets for you with: -->
  <assets>
    <index>index of the assets</index>
    <url>url from where to fetch them</url>
  </assets>
  <!-- The values can be found in the version.json file of the Minecraft version you're trying to run. -->
</game>
```

#### Example for Vanilla:

It makes sense to note that this is kinda pointless beyond developement purposes as you can just as easily use the Vanilla launcher to run the game this way.

```
<?xml version="1.0" encoding="utf-8"?>
<game>
  <name>My Minecraft game</name>
  <lock-file>game.lock</lock-file>
  <launch-version>1.19.2</launch-version>
</game>
```

#### Example for Forge:

For the Forge installation to work the Forge installer .jar or universal/client .zip must be present in the `forge` directory.
Versions of Forge with an installer won't install automatically, but will run the installer, preconfigure it and required the user to manually start the installation. This is done because the Forge developers ask not to automate the installer.

The oldest supported Forge version is for Minecraft 1.2.5. The 3 versions before that are not supported. For some older Forge versions to run library files required by FML must be present in the `forge/fmllibs` directory. (Read the text file in the directory for more info.) This is required because the FML launcher can't download them automatically anymore.

Some versions of Forge crash on (newer versions of) Java 8. The launcher includes patches for the latest releases of those versions that are applied automatically when installing.

Some version of the Forge installer install corrupted version.json files that are not parsable due to a misplaced comma. The launcher fixed them during installation.

```
<?xml version="1.0" encoding="utf-8"?>
<game>
  <name>My Minecraft game</name>
  <lock-file>game.lock</lock-file>
  <launch-forge>1.19.2-43.1.27</launch-forge>
</game>
```

The version to provide is always \<minecraft-version>-\<forge-version>. Look in `Tested versions.txt` for more examples.

#### Example for Fabric:

For the Fabric installation to work the Fabric installer must be present in the `fabric` directory.

```
<?xml version="1.0" encoding="utf-8"?>
<game>
  <name>My Minecraft game</name>
  <lock-file>game.lock</lock-file>
  <launch-fabric>1.19.2</launch-fabric>
</game>
```

PAPIPlugin Continued
==========
Forked from https://github.com/asarium/PAPIPlugin.

Origin work at 
https://forum.kerbalspaceprogram.com/index.php?/topic/38153-023-runway-papi-array-version-032/
 

This plugin adds working PAPI arrays to the runways of Kerbal Space Program.
The array will show two red and two white lights when following correct glide slope.

![UI](http://i.imgur.com/8C42Qjy.png)

Building
----------
This fork builds against Kerbal Space Program 1.12.x.

1. Add an environment variable named *KSP_PATH* that points to your KSP install root.
2. Open the solution in Visual Studio or build it with MSBuild.
3. Build the *Release* configuration.

The project references the required KSP and Unity assemblies from *<KSP_PATH>/KSP_x64_Data/Managed*
on Windows installs or *<KSP_PATH>/KSP_Data/Managed* on Linux installs.
The *KSPDIR*, *KSP_DIR*, and *KSPManagedDir* MSBuild properties are also supported.
The built DLL is copied to *assets/GameData/PAPIPlugin/Plugins* during the build.
Pass */p:RunPostBuildCopy=false* for a compile-only build that skips the local copy step.

Installation
----------
Copy the contents of *assets/GameData/PAPIPlugin* to *<KSP>/GameData/PAPIPlugin*.

Notes
----------
- The plugin uses the stock KSP application launcher for its settings UI.

Credits
----------
- asarium - Author of Runway PAPI array

- angavrilov - Contributor of Runway PAPI array

- blizzy78 - Author of Toolbar

- cybutek - Author of KSP-AVC

- Authors of CKAN

License
----------
MIT

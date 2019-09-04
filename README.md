# example-analysis-net

Here you have a basic console app that uses analysis-net as a dependency. In the console example, you have a C# program source code that is compiled in order to do a live variable analysis.

# Setup submodule dependencies (for all platforms!)
```
cd this_repository
git submodule update --init --recursive
```
# For Windows
1. Directly open Console/Console.sln using Visual Studio (it worked for Visual Studio 2019).
Visual Studio should automatically install nuget dependecies. If it doen't load them, do it yourself.
Go to the Solution Explorer tab, right click on the solution and then left click "Restore NuGet packages".

# For Linux & Mac

## More dependencies
1. Install mono-complete: https://www.mono-project.com/download/stable/
2. Install nuget: ``` sudo apt install nuget```
3. If you want an IDE (optionally but recommended) like Visual Studio install: https://www.monodevelop.com/download/

## Install nuget dependencies (only needed once):
```
cd this_repository/Console
nuget restore
```
## Command-line compilation:
```
cd this_repository/Console
msbuild 
```
## Running generated executable:
```
mono this_repository/Console/Console/bin/Debug/Console.exe
```
## Developing using MonoDevelop
Open Console/Console.sln using the IDE

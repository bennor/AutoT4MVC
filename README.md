# What's this for?

There used to be [a really sweet macro](http://stackoverflow.com/questions/2341717/can-you-do-a-runcustomtool-with-envdte-as-a-pre-build-event) for Visual Studio that would automatically run [T4MVC](http://t4mvc.codeplex.com) templates on build. Unfortunately, macro support has been removed from Visual Studio 2012, so this is an extension that will do the same thing (and more).

T4MVC templates are run under the following conditions:

* a project or solution containing the template is built
* a file in ~/Controllers is saved (in the same project)
* a file is added/removed/renamed in ~/Controllers, ~/Views, ~/Scripts or ~/Content (in the same project) 
Note: Drag/drop in the Solution Explorer will not trigger the templates to re-run, as the added/remove events are not fired.

[Chirpy](http://chirpy.codeplex.com/) and [AutoTT](https://github.com/MartinF/Dynamo.AutoTT) do the similar things, but Chirpy is overkill if all you want is your T4MVC templates built and I think AutoTT requires configuration. Also, neither of them appear to be available for VS 2012 yet.

# Installation

Just build and install the template using the VSIX file, [grab it from the Visual Studio gallery](http://visualstudiogallery.msdn.microsoft.com/8d820b76-9fc4-429f-a95f-e68ed7d3111a) or **do it the easy way** and use the 'Extension & Updates' manager in Visual Studio 2012.
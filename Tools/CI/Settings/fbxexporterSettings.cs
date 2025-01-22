using RecipeEngine.Api.Commands;
using RecipeEngine.Api.Settings;
using RecipeEngine.Modules.Wrench.Models;
using RecipeEngine.Modules.Wrench.Settings;

namespace fbxexporter.Cookbook.Settings;

public class fbxexporterSettings : AnnotatedSettingsBase
{
    // Path from the root of the repository where packages are located.
    readonly string[] PackagesRootPaths = {"./com.unity.formats.fbx"};

    // update this to list all packages in this repo that you want to release.
    Dictionary<string, PackageOptions> PackageOptions = new()
    {
        {
            "com.unity.formats.fbx",
            new PackageOptions()
            {
                ReleaseOptions = new ReleaseOptions() { IsReleasing = true },
                PackJobOptions = new PackJobOptions()
                {
                    PrePackCommands = new List<Command>
                        {new Command("Build", "./build.sh")}
                }
            }
        }
    };

    public fbxexporterSettings()
    {
        Wrench = new WrenchSettings(
            PackagesRootPaths,
            PackageOptions
        );      
    }

    public WrenchSettings Wrench { get; private set; }
}

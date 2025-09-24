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
                },
                // This is temporary, need to remove once the fix for FBX tests is landed in Trunk, see conversations in https://unity.slack.com/archives/CT99K98E8/p1756379668214119
                MaximumEditorVersion = "6000.3"
            }
        }
    };

    public fbxexporterSettings()
    {
        Wrench = new WrenchSettings(
            PackagesRootPaths,
            PackageOptions
        );
        
        Wrench.BranchNamingPattern = BranchPatterns.ReleaseSlash;
        Wrench.PvpProfilesToCheck = new HashSet<string>() { "rme", "supported" };
    }

    public WrenchSettings Wrench { get; private set; }
}

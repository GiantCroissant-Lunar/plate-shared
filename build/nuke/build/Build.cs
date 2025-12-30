using Nuke.Common;
using Nuke.Common.IO;
using UnifyBuild.Nuke;

class Build : UnifyBuildBase
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.PackProjects);

    // RootDirectory is where .nuke directory is located (build/nuke)
    AbsolutePath RepoRoot => RootDirectory / ".." / "..";

    protected override BuildContext Context
        => BuildContextLoader.FromJson(RepoRoot, "build/build.config.json");

}

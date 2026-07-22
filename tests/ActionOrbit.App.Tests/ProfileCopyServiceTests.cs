using ActionOrbit.App.Models;
using ActionOrbit.App.Services;

namespace ActionOrbit.App.Tests;

public sealed class ProfileCopyServiceTests
{
    [Fact]
    public void Copy_CreatesDeepIndependentProfile()
    {
        var source = new ProfileConfig
        {
            Id = "source",
            Name = "Source",
            Matches = [new ProfileMatch { ProcessName = "app.exe" }],
            Actions =
            [
                new OrbitAction
                {
                    Id = "folder",
                    Title = "Folder",
                    Type = "folder",
                    Children = [new OrbitAction { Id = "child", Title = "Child", Type = "open_url" }]
                }
            ]
        };

        var copy = ProfileCopyService.Copy(source, "copy", "Copy");
        copy.Matches[0].ProcessName = "changed.exe";
        copy.Actions[0].Children[0].Title = "Changed";

        Assert.Equal("copy", copy.Id);
        Assert.Equal("Copy", copy.Name);
        Assert.Equal("app.exe", source.Matches[0].ProcessName);
        Assert.Equal("Child", source.Actions[0].Children[0].Title);
    }
}

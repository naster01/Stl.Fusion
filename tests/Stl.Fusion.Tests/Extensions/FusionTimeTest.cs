using Stl.Fusion.Extensions;

namespace Stl.Fusion.Tests.Extensions;

public class FusionTimeTest : FusionTestBase
{
    public FusionTimeTest(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() { UseTestClock = true })
    { }

    [Fact]
    public async Task BasicTest()
    {
        var time = Services.GetRequiredService<IFusionTime>();

        var cTime = await Computed.Capture(_ => time.GetUtcNow());
        cTime.IsConsistent().Should().BeTrue();
        (DateTime.UtcNow - cTime.Value).Should().BeLessThan(TimeSpan.FromSeconds(1.1));
        await Delay(1.3);
        cTime.IsConsistent().Should().BeFalse();

        cTime = await Computed.Capture(_ => time.GetUtcNow(TimeSpan.FromMilliseconds(200)));
        cTime.IsConsistent().Should().BeTrue();
        await Delay(0.25);
        cTime.IsConsistent().Should().BeFalse();

        var now = DateTime.UtcNow;
        var ago = await time.GetMomentsAgo(now);
        ago.Should().Be("just now");
        await Delay(1.8);
        ago = await time.GetMomentsAgo(now);
        ago.Should().Be("1 second ago");
    }
}

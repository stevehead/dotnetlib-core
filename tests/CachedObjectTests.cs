using System;
using System.Threading;
using Xunit;

namespace Stevehead.Tests.Core;

public class CachedObjectTests
{
    private static readonly TimeSpan OneSecond = new(0, 0, 1);
    private static readonly TimeSpan HalfSecond = new(0, 0, 0, 0, 500);

    [Fact]
    public void Test_Timeout()
    {
        int cnt = 1;
        var cachedObject = new CachedObject<string>(HalfSecond, () => (cnt++).ToString());

        Thread.Sleep(OneSecond);
        Assert.Equal("1", cachedObject.Value);
        Assert.Equal("1", cachedObject.Value);
        Thread.Sleep(OneSecond);
        Assert.Equal("2", cachedObject.Value);
        Assert.Equal("2", cachedObject.Value);
        Thread.Sleep(OneSecond);
        Assert.Equal("3", cachedObject.Value);
    }

    [Fact]
    public void Test_AccessCount()
    {
        int cnt = 1;
        var cachedObject = new CachedObject<string>(2, () => (cnt++).ToString());

        Assert.Equal("1", cachedObject.Value);
        Assert.Equal("1", cachedObject.Value);
        Assert.Equal("2", cachedObject.Value);
        Assert.Equal("2", cachedObject.Value);
        Assert.Equal("3", cachedObject.Value);
        Assert.Equal("3", cachedObject.Value);
    }

    [Fact]
    public void Test_RemainingCount_AccessCount()
    {
        int cnt = 1;
        var cachedObject = new CachedObject<string>(3, () => (cnt++).ToString());

        Assert.Equal(0, cachedObject.RemainingCachedAccessCount);
        Assert.Equal("1", cachedObject.Value);
        Assert.Equal(2, cachedObject.RemainingCachedAccessCount);
        Assert.Equal("1", cachedObject.Value);
        Assert.Equal(1, cachedObject.RemainingCachedAccessCount);
        Assert.Equal("1", cachedObject.Value);
        Assert.Equal(0, cachedObject.RemainingCachedAccessCount);
    }
}

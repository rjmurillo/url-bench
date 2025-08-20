using System.Collections.Concurrent;
using System.Reflection;
using Logic.Repositories;

namespace UrlConverterPermutations;

public class CompactUrlRepositoryInternOriginalTests
{
    [Fact]
    public void InternOriginal_fast_path_returns_existing_id()
    {
        var repo = new CompactUrlRepository();
        string url = "https://fast-path.example/";
        ulong hash = InvokeHash(url);

        var (hShards, uShards, mask, _) = GetInternals(repo);
        long id = 12345L;
        hShards[(int)(hash & (uint)mask)][hash] = id;
        uShards[id & mask][id] = url;

        long ret = CallInternOriginal(repo, url);
        ret.ShouldBe(id);
    }

    [Fact]
    public void InternOriginal_true_collision_uses_collision_bucket()
    {
        var repo = new CompactUrlRepository();
        string url = "https://collision.example/";
        ulong hash = InvokeHash(url);

        var (hShards, uShards, mask, collisions) = GetInternals(repo);
        long existingId = 2001L;
        hShards[(int)(hash & (uint)mask)][hash] = existingId;
        uShards[existingId & mask][existingId] = "https://different.example/";

        var bucket = collisions.GetOrAdd(hash, _ => new ConcurrentDictionary<string, long>(StringComparer.Ordinal));
        long expected = 777L;
        bucket[url] = expected;

        long ret = CallInternOriginal(repo, url);
        ret.ShouldBe(expected);
    }

    [Fact]
    public void InternOriginal_first_claim_publishes_and_returns_new_id()
    {
        var repo = new CompactUrlRepository();
        string url = "https://first-claim.example/";
        ulong hash = InvokeHash(url);

        var (hShards, uShards, mask, _) = GetInternals(repo);
        hShards[(int)(hash & (uint)mask)].TryRemove(hash, out _);

        long ret = CallInternOriginal(repo, url);
        ret.ShouldBe(1);
        hShards[(long)(hash & (uint)mask)].TryGetValue(hash, out var publishedId).ShouldBeTrue();
        publishedId.ShouldBe(ret);
        uShards[ret & mask].TryGetValue(ret, out var storedUrl).ShouldBeTrue();
        storedUrl.ShouldBe(url);
    }

    [Fact]
    public void InternOriginal_lost_race_success_returns_raced_id()
    {
        var repo = new CompactUrlRepository();
        string url = "https://lost-race-success.example/";
        ulong hash = InvokeHash(url);

        var (hShards, uShards, mask, _) = GetInternals(repo);
        long racedId = 31415L;
        hShards[(long)(hash & (uint)mask)][hash] = racedId;
        uShards[racedId & mask][racedId] = url;

        long ret = CallInternOriginal(repo, url);
        ret.ShouldBe(racedId);
    }

    [Fact]
    public void InternOriginal_lost_race_collision_uses_bucket()
    {
        var repo = new CompactUrlRepository();
        string url = "https://lost-race-collision.example/";
        ulong hash = InvokeHash(url);

        var (hShards, uShards, mask, collisions) = GetInternals(repo);
        long racedId = 27182L;
        hShards[(long)(hash & (uint)mask)][hash] = racedId;
        uShards[racedId & mask][racedId] = "https://other.example/";

        var bucket = collisions.GetOrAdd(hash, _ => new ConcurrentDictionary<string, long>(StringComparer.Ordinal));
        long expected = 4242L;
        bucket[url] = expected;

        long ret = CallInternOriginal(repo, url);
        ret.ShouldBe(expected);
    }

    // helpers
    private static ulong InvokeHash(string value)
    {
        var mi = typeof(CompactUrlRepository).GetMethod("GetHash64", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (ulong)mi.Invoke(null, new object[] { value })!;
    }

    private static long CallInternOriginal(CompactUrlRepository repo, string url)
    {
        var mi = typeof(CompactUrlRepository).GetMethod("InternOriginal", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (long)mi.Invoke(repo, new object[] { url })!;
    }

    private static (ConcurrentDictionary<ulong, long>[] HashToId,
                    ConcurrentDictionary<long, string>[] IdToUrl,
                    int Mask,
                    ConcurrentDictionary<ulong, ConcurrentDictionary<string, long>> Collisions) GetInternals(CompactUrlRepository repo)
    {
        var state = typeof(CompactUrlRepository).GetField("_state", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(repo)!;
        var stateType = state.GetType();
        int mask = (int)stateType.GetProperty("ShardMask", BindingFlags.Public | BindingFlags.Instance)!.GetValue(state)!;
        var h = (ConcurrentDictionary<ulong, long>[])stateType.GetProperty("HashToIdShards", BindingFlags.Public | BindingFlags.Instance)!.GetValue(state)!;
        var u = (ConcurrentDictionary<long, string>[])stateType.GetProperty("IdToUrlShards", BindingFlags.Public | BindingFlags.Instance)!.GetValue(state)!;
        var collisions = (ConcurrentDictionary<ulong, ConcurrentDictionary<string, long>>)typeof(CompactUrlRepository)
            .GetField("_hashCollisions", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(repo)!;
        return (h, u, mask, collisions);
    }
}

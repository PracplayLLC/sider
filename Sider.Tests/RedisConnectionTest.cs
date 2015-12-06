﻿using System;
using System.IO;
using System.Threading;
using NUnit.Framework;

namespace Sider.Tests {
  [TestFixture]
  public class RedisConnectionTest : SiderTestBase {
    [Test]
    public void TestCtor() {
      var ms = new MemoryStream();
      var settings = RedisSettings.Default.Build()
        .Host(RandomString())
        .Build();

      Assert.Throws<ArgumentNullException>(() => new RedisConnection(null, null));
      Assert.Throws<ArgumentNullException>(() => new RedisConnection(ms, null));
      Assert.Throws<ArgumentNullException>(() => new RedisConnection(null, settings));

      var connection = new RedisConnection(ms, settings);
      Assert.AreEqual(settings, connection.Settings);
    }

    [Test, Timeout(1000)]
    public void TestConcurrentSubmit() {
      var ms = new MemoryStream();
      var connection = new RedisConnection(ms, RedisSettings.Default);
      connection.TestSubmitHook = _ => {
        // cause Monitor.Exit to never be called.
      };

      // cause connection into a locked state.
      var done = new ManualResetEventSlim(false);
      ThreadPool.QueueUserWorkItem(_ => {
        connection.Submit(BuildInvocation());
        done.Set();
      });
      done.Wait();

      // check for throw on re-entrant
      Assert.Throws<ConcurrentAccessException>(
        () => connection.Submit(BuildInvocation())
      );
      Monitor.Exit(connection);
    }
  }
}


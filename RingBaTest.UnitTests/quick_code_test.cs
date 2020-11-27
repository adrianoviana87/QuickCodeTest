using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Xunit;

namespace quick_code_test
{

    //Please fill in the implementation of the service defined below. This service is to keep track
    //of ids to return if they have been seen before. No 3rd party packages can be used and the method
    //must be thread safe to call.

    //create the implementation as efficiently as possible in both locking, memory usage, and cpu usage

    public interface IDuplicateCheckService
    {

        //checks the given id and returns if it is the first time we have seen it
        //IT IS CRITICAL that duplicates are not allowed through this system but false
        //positives can be tolerated at a maximum error rate of less than 1%
        bool IsThisTheFirstTimeWeHaveSeen(int id);

    }

    public class HighlyOptimizedThreadSafeDuplicateCheckService : IDuplicateCheckService
    {
        private readonly List<int> knownIds = new List<int>();
        private readonly object idsLock = new object(); 

        [Fact]
        public void IsThisTheFirstTimeWeHaveSeenTest()
        {
            const int threadCount = 10;
            const int idCount = 20;
            var threads = new List<Thread>();
            var threadResults = new bool[threadCount][];

            for (var threadIndex = 0; threadIndex < threadCount; threadIndex++)
            {
                Thread thread = new Thread(new ParameterizedThreadStart((object saveResults) =>
                {
                    var results = new List<bool>();
                    for (var id = 0; id < idCount; id++)
                    {
                        results.Add(IsThisTheFirstTimeWeHaveSeen(id));
                    }

                    var saveResultsFn = saveResults as Action<bool[]>;
                    saveResultsFn(results.ToArray());
                }));

                threads.Add(thread);
                var insertIndex = threadIndex;
                thread.Start(
                    new Action<bool[]>((
                        bool[] results) => threadResults[insertIndex] = results));
            }

            var finishedThreadCount = 0;
            while (finishedThreadCount < threadCount)
            {
                Thread.Sleep(500);
                finishedThreadCount = threads.Count(t => t.ThreadState == ThreadState.Stopped);
            }

            var firstTimeSeenIdsCount = threadResults.SelectMany(r => r).Count(r => r);
            Assert.Equal(20, firstTimeSeenIdsCount);
        }

        public bool IsThisTheFirstTimeWeHaveSeen(int id)
        {
            if (id < 0)
            {
                throw new ArgumentOutOfRangeException("id");
            }

            var firstTime = false;

            lock (idsLock)
            {
                firstTime = !knownIds.Contains(id);
                if (firstTime)
                {
                    knownIds.Add(id);
                }
            }

            return firstTime;
        }
    }
}

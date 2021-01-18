using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Implementation
{
    public static class DemoWorkload
    {
        public static object CheckForPrime(int i) => Map(InvokeIsPrime(i));

        public static object ListPrimesBetween(int from, int to)
        {
            var watch = Stopwatch.StartNew();
            var primes = Enumerable
                .Range(from, Math.Max(0, to - from))
                .Select(InvokeIsPrime)
                .Where(e => e.isPrime)
                .Select(Map)
                .ToArray();
            watch.Stop();

            return new {from, to, count = primes.Length, elapsed = watch.Elapsed.ToString("g"), primes};
        }

        private static bool IsPrime(int i) => i switch
        {
            < 2 => false,
            3 => true,
            4 => false,
            _ => Count().Take(i / 2).Skip(2).ToArray().All(divisor => i % divisor != 0)
        };

        [SuppressMessage("ReSharper", "IteratorNeverReturns")]
        private static IEnumerable<int> Count()
        {
            var i = 0;
            while (true) yield return i++;
        }

        private static object Map((int i, bool isPrime, string Elapsed) t) => new {t.i, t.isPrime, t.Elapsed};

        private static (int i, bool isPrime, string Elapsed) InvokeIsPrime(int i)
        {
            var watch = Stopwatch.StartNew();
            var isPrime = IsPrime(i);
            watch.Stop();
            return (i, isPrime, watch.Elapsed.ToString("g"));
        }
    }
}
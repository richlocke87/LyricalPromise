using NUnit.Framework;
using System;
using LyricalPromise;

namespace Tests {

    [TestFixture()]
    public class Test {

        [Test()]
        public void TestPromiseStartsWithPendingState() {
            var promise = new Promise<string>(resolve => { });
            Assert.AreEqual(promise.State, State.Pending);
        }

        [Test()]
        public void TestExecutorRunsImmediately() {
            var i = 1;
            var promise = new Promise<string>(resolve => i++);
            Assert.AreEqual(i, 2);
        }

        [Test()]
        public void TestPromiseResolvesImmediatelyToFulfilledState() {
            var promise = new Promise<int>(resolve => resolve(1));
            Assert.AreEqual(promise.State, State.Fulfilled);
        }

        [Test()]
        public void TestPromiseHasNoValueBeforeResolving() {
            var promise = new Promise<string>(resolve => {});
            Assert.AreEqual(promise.Value, null);
        }

        [Test()]
        public void TestPromiseResolvesImmediatelyToValue() {
            var promise = new Promise<int>(resolve => resolve(1));
            Assert.AreEqual(promise.Value, 1);
        }

        [Test()]
        public void TestPromisePassesValueToThenFunction() {
            int i = 1;
            var promise = new Promise<int>(resolve => resolve(9)).Then(value => {
                i = value;
                return value;
            });
            Assert.AreEqual(i, 9);
        }

        [Test()]
        public void TestValuesPassedThroughMultipleThens() {
            int i = 1;
            var promise = new Promise<int>(resolve => resolve(1)).Then(value => {
                return value + 1;
            }).Then(value => {
                return value + 1;
            }).Then(value => {
                i = value + 1;
                return value;
            });
            Assert.AreEqual(i, 4);
        }

        [Test()]
        public void TestPromisePendingWhenThenReturnsPromise() {
            var p1 = new Promise<int>(res => res(1));
            var p2 = p1.Then(val => new Promise<int>());
            Assert.AreEqual(State.Pending, p2.State);
        }

        [Test()]
        public void TestPromiseFulfilledWhenThenReturnsResolvedPromise() {
            var p1 = new Promise<int>(res => res(1));
            var p2 = p1.Then(val => new Promise<int>(res => res(1)));
            Assert.AreEqual(State.Fulfilled, p2.State);
        }

        [Test()]
        public void TestPromiseFulfilledWhenPendingThenPromiseIsFulfilled() {
            Action<int> p2Res = null;
            var p1 = new Promise<int>(res => res(1));
            var p2 = p1.Then(val => new Promise<int>(res => p2Res = res));
            Assert.AreEqual(State.Pending, p2.State);
            // Now resolve p2
            p2Res(1);
            Assert.AreEqual(State.Fulfilled, p2.State);
        }

        [Test()]
        public void TestThenFunctionWithNoArgs() {
            int i = 0;
            new Promise<int>(res => res(1)).Then(() => {
                i = 1;
                return 1;
            });
            Assert.AreEqual(1, i);
        }

        [Test()]
        public void TestThenFunctionWithNoArgsThatReturnsPromise() {
            int i = 0;
            new Promise<int>(res => res(1)).Then(() => {
                i = 1;
                return new Promise<int>();
            });
            Assert.AreEqual(1, i);
        }

        [Test()]
        public void TestVoidThenFunction() {
            int i = 0;
            new Promise<int>(res => res(1)).Then(val => {
                i = 1;
            });
            Assert.AreEqual(1, i);
        }

        [Test()]
        public void TestVoidThenFunctionWithNoArgs() {
            int i = 0;
            new Promise<int>(res => res(1)).Then(() => {
                i = 1;
            });
            Assert.AreEqual(1, i);
        }

        [Test()]
        public void TestValueResolvedWhenPendingThenPromiseIsFulfilled() {
            int result = 0;
            Action<int> p2Res = null;
            var p1 = new Promise<int>(res => res(1));
            var p2 = p1.Then(val => new Promise<int>(res => p2Res = res));
            p2.Then(val => result = val);

            // Until p2 is resolved, result is 0
            Assert.AreEqual(0, result);
            // Now resolve p2
            p2Res(10);
            Assert.AreEqual(10, result);
        }

        [Test()]
        public void TestThenAddedToResolvedPromiseExecutesImmediately() {
            Action<int> p1res = null;
            var p1 = new Promise<int>(res => p1res = res);
            p1res(5);
            Assert.AreEqual(State.Fulfilled, p1.State);
            // Now add a new Then
            int result = 0;
            p1.Then(val => result = val);
            Assert.AreEqual(5, result);
        }

        [Test()]
        public void TestMultipleThensCanBeAddedToSamePromise() {
            int result = 0;
            var p1 = new Promise<int>(res => res(2));
            p1.Then(val => result += val);
            p1.Then(val => result += val);
            p1.Then(val => result += val);
            Assert.AreEqual(6, result);
        }

        [Test()]
        public void TestMultipleThensAddedToSamePromiseExecuteAfterResolve() {
            int result = 0;
            Action<int> p1res = null;
            var p1 = new Promise<int>(res => p1res = res);
            p1.Then(val => result += val);
            p1.Then(val => result += val);
            p1.Then(val => result += val);
            Assert.AreEqual(0, result);
            p1res(3);
            Assert.AreEqual(9, result);
        }

        [Test()]
        public void TestDifferentValueTypesCanBeChained() {
            float[] result = new float[1];
            new Promise<int>(res => res(8)).Then(val => {
                return val > 5;
            }).Then(val => {
                return val ? "yes" : "no";
            }).Then(val => {
                result[0] = val == "yes" ? 1.1f : 8.8f;
            });
            Assert.AreEqual(1.1f, result[0]);
        }

        [Test()]
        public void TestDifferentPromiseValueTypesCanBeChained() {
            float[] result = new float[1];
            new Promise<int>(res => res(8)).Then(val => {
                return new Promise<bool>(res => res(val > 5));
            }).Then(val => {
                return new Promise<string>(res => res(val ? "yes" : "no"));
            }).Then(val => {
                result[0] = val == "yes" ? 1.1f : 8.8f;
            });
            Assert.AreEqual(1.1f, result[0]);
        }

        [Test()]
        public void TestStaticPromiseResolveGivesResolvedPromise() {
            var promise = Promise<string>.Resolve("done");
            Assert.AreEqual(State.Fulfilled, promise.State);
            Assert.AreEqual("done", promise.Value);
        }

        [Test()]
        public void TestStaticPromiseResolveChainsImmediately() {
            var promise = Promise<string>.Resolve("done").Then(value => value + " it");
            Assert.AreEqual("done it", promise.Value);
        }

        [Test()]
        public void TestResolveAllPromiseResolvesWhenAllPromisesAreResolved() {
            var resolves = new Action<int>[3];
            var p1 = new Promise<int>(res => resolves[0] = res);
            var p2 = new Promise<int>(res => resolves[1] = res);
            var p3 = new Promise<int>(res => resolves[2] = res);
            var all = Promise<int>.All(p1, p2, p3);

            Assert.AreEqual(State.Pending, all.State);
            resolves[0](0);
            Assert.AreEqual(State.Pending, all.State);
            resolves[1](1);
            Assert.AreEqual(State.Pending, all.State);
            resolves[2](2);

            // Only now all are resolved should outer Promise be fulfilled
            Assert.AreEqual(State.Fulfilled, all.State);
        }

        [Test()]
        public void TestAllPromiseChainsArrayOfResults() {
            int[] result = null;
            var resolves = new Action<int>[3];
            var p1 = new Promise<int>(res => resolves[0] = res);
            var p2 = new Promise<int>(res => resolves[1] = res);
            var p3 = new Promise<int>(res => resolves[2] = res);
            var all = Promise<int>.All(p1, p2, p3).Then(val => result = val);

            resolves[0](0);
            resolves[1](1);

            // Then not triggered until all are resolved
            Assert.AreEqual(null, result);
            resolves[2](2);

            Assert.AreEqual(new int[] { 0, 1, 2 }, result);
        }

        [Test()]
        public void TestRacePromiseResolvesWhenFirstPromisesIsResolved() {
            var resolves = new Action<int>[3];
            var p1 = new Promise<int>(res => resolves[0] = res);
            var p2 = new Promise<int>(res => resolves[1] = res);
            var p3 = new Promise<int>(res => resolves[2] = res);
            var race = Promise<int>.Race(p1, p2, p3);

            Assert.AreEqual(State.Pending, race.State);
            resolves[1](1);
            Assert.AreEqual(State.Fulfilled, race.State);
        }

        [Test()]
        public void TestRacePromiseChainsResultOfFirstResolvedPromise() {
            int result = 0;
            var resolves = new Action<int>[3];
            var p1 = new Promise<int>(res => resolves[0] = res);
            var p2 = new Promise<int>(res => resolves[1] = res);
            var p3 = new Promise<int>(res => resolves[2] = res);
            var race = Promise<int>.Race(p1, p2, p3).Then(val => result = val);

            resolves[1](1);
            Assert.AreEqual(1, race.Value);
            Assert.AreEqual(1, result);
        }

        [Test()]
        public void TestRacePromiseUnaffectedByAllPromisesResolving() {
            var thenCount = 0;
            var resolves = new Action<int>[3];
            var p1 = new Promise<int>(res => resolves[0] = res);
            var p2 = new Promise<int>(res => resolves[1] = res);
            var p3 = new Promise<int>(res => resolves[2] = res);
            var race = Promise<int>.Race(p1, p2, p3);
            race.Then(val => thenCount++);

            resolves[1](1);
            resolves[0](10);
            resolves[2](2);

            Assert.AreEqual(State.Fulfilled, race.State);
            Assert.AreEqual(1, race.Value);
            Assert.AreEqual(1, thenCount);
        }
    }
}

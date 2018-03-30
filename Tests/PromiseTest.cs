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
        public void TestSynchronousPromiseResolvesImmediatelyToFulfilledState() {
            var promise = new Promise<int>(resolve => resolve(1));
            Assert.AreEqual(promise.State, State.Fulfilled);
        }

        [Test()]
        public void TestSynchronousPromiseHasNoValueBeforeResolving() {
            var promise = new Promise<string>(resolve => {});
            Assert.AreEqual(promise.Value, null);
        }

        [Test()]
        public void TestSynchronousPromiseResolvesImmediatelyToValue() {
            var promise = new Promise<int>(resolve => resolve(1));
            Assert.AreEqual(promise.Value, 1);
        }

        [Test()]
        public void TestSychronousPromisePassesValueToThenFunction() {
            int i = 1;
            var promise = new Promise<int>(resolve => resolve(9)).Then(value => {
                i = value;
                return value;
            });
            Assert.AreEqual(i, 9);
        }
    }
}

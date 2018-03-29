using System;
using System.Collections.Generic;

namespace LyricalPromise {
    public class Promise<T> {

        private enum State {
            PENDING, FULFILLED, REJECTED
        }

        public delegate void Resolve(T t);
        public delegate void Reject(Exception e);
        public delegate void Executor(Resolve resolve, Reject reject);
        public delegate Object Then(T t);
        public delegate Executor ExecutorCreator(Promise promise);
        public State state { get; set; }
        public T value { get; }

        private T input;
        private Executor executor;
        private List<Promise<T>> downstreamPromises = new Queue<Promise<T>>();
        private List<Promise<T>> dependentPromises = new Queue<Promise<T>>();

        public Promise(Executor executor) {
            this(executor, true);
        }

        private Promise(ExecutorCreator creator) {
            this(creator(this), false);
        }

        private Promise(Executor executor, bool executeImmediately) {
            this.executor = executor;
            state = State.PENDING;

            if (executeImmediately) {
                executor(resolve, reject);
            }
        }

        public Promise<T> then(Then thenFunc) {
            return createDownstream(thenFunc);
        }

        public Promise<T> then(Action<T> thenFunc) {
            return createDownstream(thenFunc);
        }

        private Promise<T> createDownstream(Delegate thenFunc) {
            Executor downstreamExecutor = promise => (resolve, reject) => {
                Object result = null;

                if (thenFunc is Then) {
                    var castThenFunc = (Then)thenFunc;
                    result = castThenFunc(input);
                } else if (thenFunc is Action<T>) {
                    var castThenFunc = (Action<T>)thenFunc;
                    castThenFunc(input);
                }

                if (result is Promise<T>) {
                    Promise resultPromise = (Promise<T>)result;

                    if (resultPromise.state == State.FULFILLED) {
                        resolve(resultPromise.value);
                    } else if (resultPromise.state == State.PENDING) {
                        resultPromise.AddDependent(this);
                    }
                } else if (result is Object) {
                    resolve(result);
                }
            };

            Promise<T> downstream = new Promise<T>(downstreamExecutor, false);
            downstreamPromises.Enqueue(downstream);
            return downstream;
        }

        private void resolve(T t) {
            state = State.FULFILLED;
            downstreamPromises.ForEach(promise => {
                promise.Execute(t);
            });
            dependentPromises.ForEach(promise => {
                promise.resolveNow(t);
            });
        }

        private void reject(Exception e) {

        }

        void Execute(T t) {
            input = t;
            executor(resolve, reject);
        }

        void AddDependent(Promise promise) {
            dependentPromises.Add(promise);
        }

        void resolveNow(T t) {
            resolve(t);
        }

    }
}
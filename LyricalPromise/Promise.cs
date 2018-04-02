using System;
using System.Collections.Generic;
using System.Linq;

namespace LyricalPromise {

    /// <summary>
    /// The states a Promise may have. Note that Rejected is currently absent in this implementation
    /// </summary>
    public enum State {
        Pending, Fulfilled
    }

    /// <summary>
    /// Represents an asynchronous (or synchronous) operation that may or may not resolve to a value
    /// and can be chained.
    /// </summary>
    public class Promise<T> {

        /// <summary>
        /// The signature of an executor function (that a Promise is initialised with)
        /// </summary>
        public delegate void Executor(Action<T> resolve);

        /// <summary>
        /// The Promise's state
        /// </summary>
        State state;
        public State State { get { return state; } }

        /// <summary>
        /// Tha value the Promise resolves to
        /// </summary>
        T resolveValue;
        public T Value { get { return resolveValue;  } }

        /// <summary>
        /// Actions to perform when the Promise resolves. These will include resolving
        /// other Promises that depend on this one and triggering downstream actions
        /// </summary>
        readonly List<Action<T>> resolveActions = new List<Action<T>>();

        /// <summary>
        /// No-arg constructor.
        /// </summary>
        public Promise() {
            state = State.Pending;
        }

        /// <summary>
        /// Constructor that takes an Executor function. The function is executed before
        /// the constructor returns.
        /// </summary>
        public Promise(Executor executor) {
            CreatePromise(executor);
        }

        /// <summary>
        /// Sets Promise state to Pending and runs the Executor function
        /// </summary>
        void CreatePromise(Executor executor) {
            state = State.Pending;
            executor(DoResolve);
        }

        /// <summary>
        /// A follow-on action that receives the chained value and returns a new value
        /// </summary>
        public Promise<S> Then<S>(Func<T, S> thenFunc) {
            Promise<S> downstream = new Promise<S>();
            AddResolveAction(resolveValue => {
                S thenValue = thenFunc(resolveValue);
                downstream.DoResolve(thenValue);
            });
            return downstream;
        }

        /// <summary>
        /// A follow-on action that receives the chained value and returns a Promise
        /// </summary>
        public Promise<S> Then<S>(Func<T, Promise<S>> thenFunc) {
            Promise<S> downstream = new Promise<S>();
            AddResolveAction(resolveValue => {
                Promise<S> thenPromise = thenFunc(resolveValue);
                thenPromise.AddResolveAction(thenResolveValue => {
                    downstream.DoResolve(thenResolveValue);
                });
            });
            return downstream;
        }

        /// <summary>
        /// A follow-on action with no args that returns a new value
        /// </summary>
        public Promise<S> Then<S>(Func<S> thenFunc) {
            Promise<S> downstream = new Promise<S>();
            AddResolveAction(resolveValue => {
                S thenValue = thenFunc();
                downstream.DoResolve(thenValue);
            });
            return downstream;
        }

        /// <summary>
        /// A follow-on action with no args that returns a Promise
        /// </summary>
        public Promise<S> Then<S>(Func<Promise<S>> thenFunc) {
            Promise<S> downstream = new Promise<S>();
            AddResolveAction(resolveValue => {
                Promise<S> thenPromise = thenFunc();
                thenPromise.AddResolveAction(thenResolveValue => {
                    downstream.DoResolve(thenResolveValue);
                });
            });
            return downstream;
        }

        /// <summary>
        /// A follow-on action that receives the chained value and returns nothing
        /// </summary>
        public Promise<object> Then(Action<T> thenFunc) {
            Promise<object> downstream = new Promise<object>();
            AddResolveAction(resolveValue => {
                thenFunc(resolveValue);
                downstream.DoResolve(null);
            });
            return downstream;
        }

        /// <summary>
        /// A follow-on action with no args that returns nothing
        /// </summary>
        public Promise<object> Then(Action thenFunc) {
            Promise<object> downstream = new Promise<object>();
            AddResolveAction(resolveValue => {
                thenFunc();
                downstream.DoResolve(null);
            });
            return downstream;
        }

        /// <summary>
        /// The resolve function that is executed when the Promise resolves. Sets this
        /// Promise's state to Fulfilled and executes all other resolve actions
        /// </summary>
        void DoResolve(T t) {
            state = State.Fulfilled;
            resolveValue = t;

            resolveActions.ForEach(resolveAction => resolveAction(t));
        }

        /// <summary>
        /// Adds a resolve action to be executed when the Promise resolves.
        /// </summary>
        void AddResolveAction(Action<T> resolveAction) {
            if (state == State.Pending) {
                resolveActions.Add(resolveAction);
            } else if (state == State.Fulfilled) {
                resolveAction(resolveValue);
            }
        }

        /// <summary>
        /// Returns a Promise resolved with the supplied value.
        /// </summary>
        public static Promise<T> Resolve(T value) {
            return new Promise<T>(res => res(value));
        }

        /// <summary>
        /// Returns a Promise that resolves only when all the provided Promises
        /// have resolved
        /// </summary>
        public static Promise<T[]> All(params Promise<T>[] promises) {
            return new Promise<T[]>(resolve => {
                foreach (var prom in promises) {
                    prom.AddResolveAction(resolveValue => {
                        // Resolve if all are fulfilled
                        if (promises.All(p => p.State == State.Fulfilled)) {
                            var valueArray = promises.Select(p => p.resolveValue).ToArray();
                            resolve(valueArray);
                        }
                    });
                }
            });
        }

        /// <summary>
        /// Returns a Promise that resolves as soon as any of the supplied Promises
        /// resolve. The Promise receives the value of the first Promise to resolve
        /// </summary>
        public static Promise<T> Race(params Promise<T>[] promises) {
            return new Promise<T>(resolve => {
                foreach (var prom in promises) {
                    prom.AddResolveAction(resolveValue => {
                        // Resolve if only one is fulfilled
                        var numResolved = promises.Count(p => p.State == State.Fulfilled);
                        if (numResolved == 1) {
                            resolve(resolveValue);
                        }
                    });
                }
            });
        }

    }
}
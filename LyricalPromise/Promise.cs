using System;
using System.Collections.Generic;

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
            executor(Resolve);
        }

        /// <summary>
        /// A follow-on action that receives the chained value and returns a new value
        /// </summary>
        public Promise<S> Then<S>(Func<T, S> thenFunc) {
            Promise<S> downstream = new Promise<S>();
            AddResolveAction(resolveValue => {
                S thenValue = thenFunc(resolveValue);
                downstream.Resolve(thenValue);
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
                    downstream.Resolve(thenResolveValue);
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
                downstream.Resolve(thenValue);
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
                    downstream.Resolve(thenResolveValue);
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
                downstream.Resolve(null);
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
                downstream.Resolve(null);
            });
            return downstream;
        }

        /// <summary>
        /// The resolve function that is executed when the Promise resolves. Sets this
        /// Promise's state to Fulfilled and executes all other resolve actions
        /// </summary>
        void Resolve(T t) {
            state = State.Fulfilled;
            resolveValue = t;

            resolveActions.ForEach(resolveAction => resolveAction(t));
        }

        /// <summary>
        /// Adds a resolve action to be executed when the Promise resolves.
        /// </summary>
        /// <param name="resolveAction">Resolve action.</param>
        void AddResolveAction(Action<T> resolveAction) {
            if (state == State.Pending) {
                resolveActions.Add(resolveAction);
            } else if (state == State.Fulfilled) {
                resolveAction(resolveValue);
            }
        }

    }
}
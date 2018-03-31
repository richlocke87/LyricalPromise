using System;
using System.Collections.Generic;

namespace LyricalPromise {
    public enum State {
        Pending, Fulfilled
    }

    public class Promise<T> {

        public delegate void Executor(Action<T> resolve);

        State state;
        public State State { get { return state; } }

        T resolveValue;
        public T Value { get { return resolveValue;  } }

        readonly List<Action<T>> resolveActions = new List<Action<T>>();

        public Promise() {
            state = State.Pending;
        }

        public Promise(Executor executor) {
            CreatePromise(executor);
        }

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

        void Resolve(T t) {
            state = State.Fulfilled;
            resolveValue = t;

            resolveActions.ForEach(resolveAction => resolveAction(t));
        }

        void AddResolveAction(Action<T> resolveAction) {
            if (state == State.Pending) {
                resolveActions.Add(resolveAction);
            } else if (state == State.Fulfilled) {
                resolveAction(resolveValue);
            }
        }

    }
}
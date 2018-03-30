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

        public Promise<S> Then<S>(Func<T, S> thenFunc) {
            Promise<S> downstream = new Promise<S>();

            AddResolveAction(resolveValue => {
                S thenValue = thenFunc(resolveValue);
                downstream.Resolve(thenValue);
            });

            return downstream;
        }

        public Promise<S> Then<S>(Func<T, Promise<S>> thenFunc) {
            Promise<S> downstream = new Promise<S>();

            Action<T> resolveAction = resolveValue => {
                Promise<S> thenPromise = thenFunc(resolveValue);
                thenPromise.AddResolveAction(thenResolveValue => {
                    downstream.Resolve(thenResolveValue);
                });
            };

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
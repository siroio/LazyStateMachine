using System;
using System.Collections.Generic;

#nullable enable

namespace LazyStateMachine
{
    public interface IInitializeState
    {
        void InitializeState();
    }

    public interface IOnEnter
    {
        void OnEnter();
    }

    public interface IOnFixedUpdate
    {
        void OnFixedUpdate();
    }

    public interface IOnUpdate
    {
        void OnUpdate();
    }

    public interface IOnLateUpdate
    {
        void OnLateUpdate();
    }

    public interface IOnExit
    {
        void OnExit();
    }

    public abstract class LazyStateBase<T>
    {
        protected T Parent { get; }

        public LazyStateBase(T parent)
        {
            Parent = parent;
        }
    }

    public sealed class LazyStateMachine<T, E> where E : struct, Enum
    {
        public E? CurrentStateID { get; private set; }
        public E? PrevStateID { get; private set; }

        #region Members
        private readonly Dictionary<E, LazyStateBase<T>> stateInstances = new();
        private readonly Dictionary<E, IInitializeState> initializeFunctions = new();
        private readonly Dictionary<E, IOnEnter> enterFunctions = new();
        private readonly Dictionary<E, IOnUpdate> updateFunctions = new();
        private readonly Dictionary<E, IOnFixedUpdate> fixedUpdateFunctions = new();
        private readonly Dictionary<E, IOnLateUpdate> lateUpdateFunctions = new();
        private readonly Dictionary<E, IOnExit> exitFunctions = new();
        private E initialStateId;
        #endregion

        public event Action<E?, E>? OnStateChanged;

        public void Initialize()
        {
            foreach (var state in initializeFunctions.Values)
            {
                state.InitializeState();
            }
            initializeFunctions.Clear();

            ChangeState(initialStateId);
        }

        public void SetInitialState(E stateId) => initialStateId = stateId;

        public bool RegisterState(E stateID, LazyStateBase<T> state)
        {
            if (stateInstances.ContainsKey(stateID)) return false;

            stateInstances[stateID] = state;

            if (state is IInitializeState initialize)
            {
                initializeFunctions[stateID] = initialize;
            }
            if (state is IOnEnter enter)
            {
                enterFunctions[stateID] = enter;
            }
            if (state is IOnUpdate update)
            {
                updateFunctions[stateID] = update;
            }
            if (state is IOnFixedUpdate fixedUpdate)
            {
                fixedUpdateFunctions[stateID] = fixedUpdate;
            }
            if (state is IOnLateUpdate lateUpdate)
            {
                lateUpdateFunctions[stateID] = lateUpdate;
            }
            if (state is IOnExit exit)
            {
                exitFunctions[stateID] = exit;
            }

            return true;
        }

        public bool ChangeState(E eventId)
        {
            if (EqualsState(CurrentStateID, eventId)) return false;
            if (!stateInstances.ContainsKey(eventId)) return false;

            if (CurrentStateID is not null && exitFunctions.TryGetValue(CurrentStateID.Value, out var exitState))
            {
                exitState?.OnExit();
            }

            PrevStateID = CurrentStateID;
            CurrentStateID = eventId;

            if (enterFunctions.TryGetValue(eventId, out var enterState))
            {
                enterState?.OnEnter();
            }

            OnStateChanged?.Invoke(PrevStateID, eventId);

            return true;
        }

        public void Update()
        {
            if (CurrentStateID is not null && updateFunctions.TryGetValue(CurrentStateID.Value, out var state))
            {
                state?.OnUpdate();
            }
        }
        public void FixedUpdate()
        {
            if (CurrentStateID is not null && fixedUpdateFunctions.TryGetValue(CurrentStateID.Value, out var state))
            {
                state?.OnFixedUpdate();
            }
        }
        public void LateUpdate()
        {
            if (CurrentStateID is not null && lateUpdateFunctions.TryGetValue(CurrentStateID.Value, out var state))
            {
                state?.OnLateUpdate();
            }
        }

        public bool ResetState(E stateId)
        {
            if (!stateInstances.ContainsKey(stateId)) return false;

            if (exitFunctions.TryGetValue(stateId, out var exitState))
                exitState?.OnExit();
            if (enterFunctions.TryGetValue(stateId, out var enterState))
                enterState?.OnEnter();

            return true;
        }

        public void Dispose()
        {
            enterFunctions.Clear();
            updateFunctions.Clear();
            fixedUpdateFunctions.Clear();
            lateUpdateFunctions.Clear();
            exitFunctions.Clear();
            stateInstances.Clear();
        }

        private bool EqualsState(E? state1, E state2) => state1.HasValue && EqualityComparer<E>.Default.Equals(state1.Value, state2);
    }
}

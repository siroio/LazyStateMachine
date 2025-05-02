using System;
using System.Runtime.CompilerServices;

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

    [Flags]
    public enum StateInterfaceMask : byte
    {
        None = 0,
        Initialize = 1 << 0,
        OnEnter = 1 << 1,
        OnUpdate = 1 << 2,
        OnFixedUpdate = 1 << 3,
        OnLateUpdate = 1 << 4,
        OnExit = 1 << 5,
    }

    public sealed class LazyStateMachine<TContext, TEnum>
        where TEnum : unmanaged, Enum
    {
        public abstract class State
        {
            protected TContext? Parent { get; private set; }

            internal void SetParent(TContext parent) => Parent = parent;
        }

        private readonly struct StateRecord
        {
            public readonly State State;
            public readonly StateInterfaceMask Mask;

            public StateRecord(State state, StateInterfaceMask mask)
            {
                State = state;
                Mask = mask;
            }
        }


        private readonly Memory<StateRecord> stateRecords;
        private int currentStateID = -1;
        private int prevStateID = -1;

        public int CurrentStateID => currentStateID;
        public int PrevStateID => prevStateID;

        public event Action<int, int>? OnStateChanged;

        public LazyStateMachine()
        {
            var count = Enum.GetValues(typeof(TEnum)).Length;
            stateRecords = new Memory<StateRecord>(new StateRecord[count]);
        }

        public void RegisterState<TState>(TEnum id, TContext context)
            where TState : State, new()
        {
            var i = ToInt(id);

            var state = new TState();
            state.SetParent(context);

            var mask = GetStateMask(state);
            stateRecords.Span[i] = new StateRecord(state, mask);
        }

        public void Initialize()
        {
            for (var i = 0; i < stateRecords.Length; i++)
            {
                var record = stateRecords.Span[i];
                if ((record.Mask & StateInterfaceMask.Initialize) != 0)
                    ((IInitializeState)record.State).InitializeState();
            }
        }

        public bool ChangeState(TEnum newState)
        {
            int i = ToInt(newState);
            if (i >= stateRecords.Length || i == currentStateID)
                return false;

            if (currentStateID >= 0 && (stateRecords.Span[currentStateID].Mask & StateInterfaceMask.OnExit) != 0)
                ((IOnExit)stateRecords.Span[currentStateID].State).OnExit();

            prevStateID = currentStateID;
            currentStateID = i;

            if ((stateRecords.Span[currentStateID].Mask & StateInterfaceMask.OnEnter) != 0)
                ((IOnEnter)stateRecords.Span[currentStateID].State).OnEnter();

            OnStateChanged?.Invoke(prevStateID, currentStateID);
            return true;
        }

        public void Update()
        {
            if (currentStateID >= 0 && (stateRecords.Span[currentStateID].Mask & StateInterfaceMask.OnUpdate) != 0)
                ((IOnUpdate)stateRecords.Span[currentStateID].State).OnUpdate();
        }

        public void FixedUpdate()
        {
            if (currentStateID >= 0 && (stateRecords.Span[currentStateID].Mask & StateInterfaceMask.OnFixedUpdate) != 0)
                ((IOnFixedUpdate)stateRecords.Span[currentStateID].State).OnFixedUpdate();
        }

        public void LateUpdate()
        {
            if (currentStateID >= 0 && (stateRecords.Span[currentStateID].Mask & StateInterfaceMask.OnLateUpdate) != 0)
                ((IOnLateUpdate)stateRecords.Span[currentStateID].State).OnLateUpdate();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe int ToInt(TEnum value)
        {
            return *(int*)&value;
        }

        private static StateInterfaceMask GetStateMask<TState>(TState state) where TState : State
        {
            var mask = StateInterfaceMask.None;

            if (state is IInitializeState) mask |= StateInterfaceMask.Initialize;
            if (state is IOnEnter) mask |= StateInterfaceMask.OnEnter;
            if (state is IOnUpdate) mask |= StateInterfaceMask.OnUpdate;
            if (state is IOnFixedUpdate) mask |= StateInterfaceMask.OnFixedUpdate;
            if (state is IOnLateUpdate) mask |= StateInterfaceMask.OnLateUpdate;
            if (state is IOnExit) mask |= StateInterfaceMask.OnExit;

            return mask;
        }
    }
}

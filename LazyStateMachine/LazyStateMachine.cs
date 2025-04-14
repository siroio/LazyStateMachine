using System;
using System.Collections.Generic;
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

    public abstract class LazyStateBase<T>
    {
        protected T Parent { get; }

        public LazyStateBase(T parent)
        {
            Parent = parent;
        }
    }

    public sealed class LazyStateMachine<T>
    {
        public int? CurrentStateID { get; private set; }
        public int? PrevStateID { get; private set; }

        #region Members
        private readonly Dictionary<int, LazyStateBase<T>> stateInstances = new();
        private readonly Dictionary<int, IInitializeState> initializeFunctions = new();
        private readonly Dictionary<int, IOnEnter> enterFunctions = new();
        private readonly Dictionary<int, IOnUpdate> updateFunctions = new();
        private readonly Dictionary<int, IOnFixedUpdate> fixedUpdateFunctions = new();
        private readonly Dictionary<int, IOnLateUpdate> lateUpdateFunctions = new();
        private readonly Dictionary<int, IOnExit> exitFunctions = new();
        private int initialStateId;
        #endregion

        public event Action<int?, int>? OnStateChanged;

        public void Initialize()
        {
            foreach (var state in initializeFunctions.Values)
            {
                state.InitializeState();
            }
            initializeFunctions.Clear();

            ChangeState(initialStateId);
        }

        public void SetInitialState(int stateID) => initialStateId = stateID;

        public bool RegisterState<E>(E stateID, LazyStateBase<T> state) where E : Enum
        {
            return RegisterState(ToInt(stateID), state);
        }

        public bool RegisterState(int stateID, LazyStateBase<T> state)
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

        public bool ChangeState<E>(E stateID) where E : Enum
        {
            return ChangeState(ToInt(stateID));
        }

        public bool ChangeState(int stateID)
        {
            if (CurrentStateID == stateID) return false;
            if (!stateInstances.ContainsKey(stateID)) return false;

            if (CurrentStateID is not null && exitFunctions.TryGetValue(CurrentStateID.Value, out var exitState))
            {
                exitState?.OnExit();
            }

            PrevStateID = CurrentStateID;
            CurrentStateID = stateID;

            if (enterFunctions.TryGetValue(stateID, out var enterState))
            {
                enterState?.OnEnter();
            }

            OnStateChanged?.Invoke(PrevStateID, stateID);

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

        public bool ResetState(int stateID)
        {
            if (!stateInstances.ContainsKey(stateID)) return false;

            if (exitFunctions.TryGetValue(stateID, out var exitState))
                exitState?.OnExit();
            if (enterFunctions.TryGetValue(stateID, out var enterState))
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ToInt<E>(E value) where E : Enum
        {
            if (Enum.GetUnderlyingType(typeof(E)) != typeof(int))
                throw new InvalidOperationException();
            return Unsafe.As<E, int>(ref value);
        }
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

    internal readonly struct StateRecord<TContext>
    {
        public readonly LazyStateBase<TContext> State;
        public readonly StateInterfaceMask Mask;

        public StateRecord(LazyStateBase<TContext> state, StateInterfaceMask mask)
        {
            State = state;
            Mask = mask;
        }
    }

    public sealed class GCFreeStateMachine<TContext, TEnum>
        where TEnum : unmanaged, Enum
    {
        private readonly Memory<StateRecord<TContext>> stateRecords;
        private int currentStateID = -1;
        private int prevStateID = -1;
        private readonly TContext context;

        public int CurrentStateID => currentStateID;
        public int PrevStateID => prevStateID;

        public event Action<int, int>? OnStateChanged;

        public GCFreeStateMachine(TContext ctx)
        {
            context = ctx;
            var count = Enum.GetValues(typeof(TEnum)).Length;
            stateRecords = new Memory<StateRecord<TContext>>(new StateRecord<TContext>[count]);
        }

        public void Register<TState>(TEnum id, TState state) where TState : LazyStateBase<TContext>
        {
            int i = ToInt(id);
            if (stateRecords.Span[i].State != null)
            {
                return;
            }

            // 状態とインターフェースのマスクをメモリに保存
            var mask = GetStateMask(state);
            stateRecords.Span[i] = new StateRecord<TContext>(state, mask);
        }

        public void Initialize()
        {
            // `Memory<T>` の `Span` を使用して効率的にアクセス
            for (int i = 0; i < stateRecords.Length; i++)
            {
                var record = stateRecords.Span[i];
                if ((record.Mask & StateInterfaceMask.Initialize) != 0)
                    ((IInitializeState)record.State).InitializeState();
            }
        }

        public bool ChangeState(TEnum newState)
        {
            int i = ToInt(newState);
            if (i >= stateRecords.Length || stateRecords.Span[i].State == null || i == currentStateID)
                return false;

            // 既存の状態のExit処理
            if (currentStateID >= 0 && (stateRecords.Span[currentStateID].Mask & StateInterfaceMask.OnExit) != 0)
                ((IOnExit)stateRecords.Span[currentStateID].State).OnExit();

            // 新しい状態のEnter処理
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

        private static StateInterfaceMask GetStateMask<TState>(TState state) where TState : LazyStateBase<TContext>
        {
            StateInterfaceMask mask = StateInterfaceMask.None;

            // インターフェースが実装されているかどうかをチェックする部分
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

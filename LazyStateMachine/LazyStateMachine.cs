#define USE_FUNCTION_POINTER

using System;
using System.Runtime.CompilerServices;

#nullable enable

namespace LazyStateMachine
{
    public interface IInitializeState { void InitializeState(); }
    public interface IOnEnter { void OnEnter(); }
    public interface IOnFixedUpdate { void OnFixedUpdate(); }
    public interface IOnUpdate { void OnUpdate(); }
    public interface IOnLateUpdate { void OnLateUpdate(); }
    public interface IOnExit { void OnExit(); }

    public sealed class LazyStateMachine<TParent, TEnum>
        where TEnum : unmanaged, Enum
    {
        public abstract class State
        {
            protected TParent Parent { get; private set; } = default!;
            internal void SetParent(TParent parent) => Parent = parent;
        }

#if !USE_FUNCTION_POINTER
        private sealed class CallTable
        {
            public Action<State>? Initialize;
            public Action<State>? Enter;
            public Action<State>? Update;
            public Action<State>? Fixed;
            public Action<State>? Late;
            public Action<State>? Exit;
        }
#endif

#if USE_FUNCTION_POINTER
        private sealed unsafe class StateRecord
#else
        private sealed class StateRecord
#endif
        {
            public readonly State Instance;
#if USE_FUNCTION_POINTER
            public readonly delegate*<State, void> Initialize;
            public readonly delegate*<State, void> Enter;
            public readonly delegate*<State, void> Update;
            public readonly delegate*<State, void> Fixed;
            public readonly delegate*<State, void> Late;
            public readonly delegate*<State, void> Exit;

            public StateRecord(State inst,
                delegate*<State, void> init,
                delegate*<State, void> enter,
                delegate*<State, void> update,
                delegate*<State, void> fixedUpd,
                delegate*<State, void> late,
                delegate*<State, void> exit)
            {
                Instance = inst;
                Initialize = init;
                Enter = enter;
                Update = update;
                Fixed = fixedUpd;
                Late = late;
                Exit = exit;
            }
#else
            public readonly CallTable Table;
            public StateRecord(State inst, CallTable table) { Instance = inst; Table = table; }
#endif
        }

        private static readonly int StateCount = Enum.GetValues(typeof(TEnum)).Length;
        private readonly StateRecord?[] records = new StateRecord[StateCount];
        private TEnum cur;
        private TEnum prev;

        public int CurrentStateID => ToInt(cur);
        public int PrevStateID => ToInt(prev);

        public event Action<int, int>? OnStateChanged;

        public void RegisterState<TState>(TEnum id, TParent ctx)
            where TState : State, new()
        {
            int idx = ToInt(id);
            var state = new TState();
            state.SetParent(ctx);

#if USE_FUNCTION_POINTER
            unsafe
            {
                delegate*<State, void> init = state is IInitializeState ? &InvokeInitialize : null;
                delegate*<State, void> enter = state is IOnEnter ? &InvokeEnter : null;
                delegate*<State, void> up = state is IOnUpdate ? &InvokeUpdate : null;
                delegate*<State, void> fix = state is IOnFixedUpdate ? &InvokeFixed : null;
                delegate*<State, void> late = state is IOnLateUpdate ? &InvokeLate : null;
                delegate*<State, void> exit = state is IOnExit ? &InvokeExit : null;
                records[idx] = new StateRecord(state, init, enter, up, fix, late, exit);
            }
#else
            var table = new CallTable();
            if (state is IInitializeState) table.Initialize = InvokeInitialize;
            if (state is IOnEnter) table.Enter = InvokeEnter;
            if (state is IOnUpdate) table.Update = InvokeUpdate;
            if (state is IOnFixedUpdate) table.Fixed = InvokeFixed;
            if (state is IOnLateUpdate) table.Late = InvokeLate;
            if (state is IOnExit) table.Exit = InvokeExit;
            records[idx] = new StateRecord(state, table);
#endif
        }

        public void Initialize()
        {
#if USE_FUNCTION_POINTER
            unsafe
            {
                foreach (var rec in records)
                {
                    if (rec != null) InvokePtr(rec.Initialize, rec.Instance);
                }
            }
#else
            foreach (var rec in records)
                rec?.Table.Initialize?.Invoke(rec.Instance);
#endif
        }

        public bool ChangeState(TEnum next)
        {
            int nextIdx = ToInt(next);
            int curIdx = ToInt(cur);
            if (nextIdx >= records.Length || nextIdx == curIdx) return false;

#if USE_FUNCTION_POINTER
            unsafe
            {
                var rec = records[curIdx];
                if (rec != null) InvokePtr(rec.Exit, rec.Instance);
            }
#else
            records[curIdx]?.Table.Exit?.Invoke(records[curIdx].Instance);
#endif

            prev = cur;
            cur = next;

#if USE_FUNCTION_POINTER
            unsafe
            {
                var rec = records[nextIdx];
                if (rec != null) InvokePtr(rec.Enter, rec.Instance);
            }
#else
            records[nextIdx]?.Table.Enter?.Invoke(records[nextIdx].Instance);
#endif
            var callback = OnStateChanged;
            callback?.Invoke(ToInt(prev), ToInt(cur));
            return true;
        }

#if USE_FUNCTION_POINTER
        public unsafe void Update() => InvokeCurrentUnsafe(rec => (IntPtr)rec.Update);
        public unsafe void FixedUpdate() => InvokeCurrentUnsafe(rec => (IntPtr)rec.Fixed);
        public unsafe void LateUpdate() => InvokeCurrentUnsafe(rec => (IntPtr)rec.Late);

        private unsafe void InvokeCurrentUnsafe(Func<StateRecord, IntPtr> selector)
        {
            int idx = ToInt(cur);
            if (idx < 0 || idx >= records.Length) return;
            var rec = records[idx];
            if (rec == null) return;

            var ptr = (delegate*<State, void>)selector(rec);
            InvokePtr(ptr, rec.Instance);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void InvokePtr(delegate*<State, void> ptr, State? inst)
        {
            if ((nint)ptr != 0 && inst != null) ptr(inst);
        }
#else
        public void Update() => records[ToInt(cur)]?.Table.Update?.Invoke(records[ToInt(cur)]!.Instance);
        public void FixedUpdate() => records[ToInt(cur)]?.Table.Fixed?.Invoke(records[ToInt(cur)]!.Instance);
        public void LateUpdate() => records[ToInt(cur)]?.Table.Late?.Invoke(records[ToInt(cur)]!.Instance);
#endif

        [MethodImpl(MethodImplOptions.AggressiveInlining)] private static void InvokeInitialize(State s) => ((IInitializeState)s).InitializeState();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] private static void InvokeEnter(State s) => ((IOnEnter)s).OnEnter();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] private static void InvokeUpdate(State s) => ((IOnUpdate)s).OnUpdate();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] private static void InvokeFixed(State s) => ((IOnFixedUpdate)s).OnFixedUpdate();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] private static void InvokeLate(State s) => ((IOnLateUpdate)s).OnLateUpdate();
        [MethodImpl(MethodImplOptions.AggressiveInlining)] private static void InvokeExit(State s) => ((IOnExit)s).OnExit();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ToInt(TEnum value)
        {
            return Unsafe.As<TEnum, int>(ref value);
        }
    }
}
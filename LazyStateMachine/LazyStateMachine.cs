using System;
using System.Collections.Generic;

namespace LazyStateMachine
{
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

    public class LazyStateBase
    {
        public virtual void InitializeState()
        { }
    }

    public sealed class LazyStateMachine<T, E> where E : Enum
    {
        #region Property
        public T Parent { get; private set; }
        public E CurrentStateID { get; private set; }
        #endregion

        #region Members
        private readonly Dictionary<E, LazyStateBase> stateInstances;
        private readonly Dictionary<E, Action> enterFunctions;
        private readonly Dictionary<E, Action> updateFunctions;
        private readonly Dictionary<E, Action> fixedUpdateFunctions;
        private readonly Dictionary<E, Action> lateUpdateFunctions;
        private readonly Dictionary<E, Action> exitFunctions;
        private E initialStateId = default;
        #endregion

        public LazyStateMachine()
        {
            stateInstances = new Dictionary<E, LazyStateBase>();
            enterFunctions = new Dictionary<E, Action>();
            updateFunctions = new Dictionary<E, Action>();
            fixedUpdateFunctions = new Dictionary<E, Action>();
            lateUpdateFunctions = new Dictionary<E, Action>();
            exitFunctions = new Dictionary<E, Action>();
        }

        public bool Initialize(T parent)
        {
            if (parent == null)
            {
                return false;
            }

            Parent = parent;

            foreach(var state in stateInstances)
            {
                state.Value.InitializeState();
            }

            ChangeState(initialStateId);

            return true;
        }

        public void SetInitialState(E stateId)
        {
            initialStateId = stateId;
        }

        public void RegisterState(E stateID, LazyStateBase state)
        {
            stateInstances[stateID] = state;

            if (state is IOnEnter enterState)
                enterFunctions[stateID] = enterState.OnEnter;

            if (state is IOnUpdate updateState)
                updateFunctions[stateID] = updateState.OnUpdate;

            if (state is IOnFixedUpdate fixedUpdateState)
                fixedUpdateFunctions[stateID] = fixedUpdateState.OnFixedUpdate;

            if (state is IOnLateUpdate lateUpdateState)
                lateUpdateFunctions[stateID] = lateUpdateState.OnLateUpdate;

            if (state is IOnExit exitState)
                exitFunctions[stateID] = exitState.OnExit;
        }

        public void ChangeState(E eventId)
        {
            // 現在のステートと遷移先のステートが同じ場合は処理をスキップ
            if (EqualsState(CurrentStateID, eventId))
            {
                return; // 同じステートに遷移しようとしているので何もしない
            }

            // 最初の状態遷移時には Exit を呼ばないようにする
            if (EqualsState(CurrentStateID, default))
            {
                // 現在のステートの Exit を呼び出す
                exitFunctions[CurrentStateID]?.Invoke();
            }

            // 現在のステートを更新
            CurrentStateID = eventId;

            // 新しいステートの Enter を呼び出す
            enterFunctions[eventId]?.Invoke();
        }


        public void Update()
        {
            updateFunctions[CurrentStateID]?.Invoke();
        }

        public void FixedUpdate()
        {
            fixedUpdateFunctions[CurrentStateID]?.Invoke();
        }

        public void LateUpdate()
        {
            lateUpdateFunctions[CurrentStateID]?.Invoke();
        }

        private bool EqualsState(E state1, E state2)
        {
            return EqualityComparer<E>.Default.Equals(state1, state2);
        }
    }
}

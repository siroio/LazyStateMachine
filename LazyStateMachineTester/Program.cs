using System;
using System.Diagnostics;
using LazyStateMachine;
using LazyStateMachineTester;

[GenerateLazyState]
public partial class IdleState : LazyStateMachine<Player, Player.State>.State
{
    public void OnEnter() => Console.WriteLine("[IdleState] Enter");
    public void OnUpdate() => Console.WriteLine("[IdleState] Update");
    public void OnExit() => Console.WriteLine("[IdleState] Exit");
}

namespace LazyStateMachineTester
{
    [GenerateLazyState]
    public partial class WalkState : LazyStateMachine<Player, Player.State>.State
    {
        public void OnEnter() => Console.WriteLine("[WalkState] Enter");
        public void OnUpdate() => Console.WriteLine("[WalkState] Update");
        public void OnExit() => Console.WriteLine("[WalkState] Exit");

    }

    [GenerateLazyState]
    public partial class RunState : LazyStateMachine<Player, Player.State>.State
    {
        public void OnEnter() => Console.WriteLine("[RunState] Enter");
        public void OnUpdate() => Console.WriteLine("[RunState] Update");
        public void OnExit() => Console.WriteLine("[RunState] Exit");
    }

    public class Player
    {
        public enum State : byte
        {
            Idle,
            Walk,
            Run,
        }

        private readonly LazyStateMachine<Player, State> sm;
        public Player()
        {
            sm = new();
            sm.RegisterState<IdleState>(State.Idle, this);
            sm.RegisterState<WalkState>(State.Walk, this);
            sm.RegisterState<RunState>(State.Run, this);

            sm.Initialize();
        }

        public void Bench()
        {


            sm.Update();

            // 状態遷移テスト
            sm.ChangeState(State.Walk);
            sm.ChangeState(State.Run);
            sm.ChangeState(State.Idle);

            // 未登録の状態を試す（エラーハンドリング確認）
            sm.ChangeState((State)255);
        }
    }

    public static class Program
    {
        public static void Main()
        {
            var player = new Player();
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            for (int i = 0; i < 1000; i++)
            {
                player.Bench();
            }
            stopwatch.Stop();

            Console.WriteLine($"Total Player initialization time: {stopwatch.ElapsedMilliseconds} ms");
        }
    }
}

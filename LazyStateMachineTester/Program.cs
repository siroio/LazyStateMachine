using System;
using System.Diagnostics;
using LazyStateMachine;

namespace LazyStateMachineTester
{
    public partial class IdleState(Player parent) : LazyStateBase<Player>(parent)
    {
        public void OnEnter() => Console.WriteLine("[IdleState] Enter");
        public void OnUpdate() => Console.WriteLine("[IdleState] Update");
        public void OnExit() => Console.WriteLine("[IdleState] Exit");
    }

    public partial class WalkState(Player parent) : LazyStateBase<Player>(parent)
    {
        public void OnEnter() => Console.WriteLine("[WalkState] Enter");
        public void OnUpdate() => Console.WriteLine("[WalkState] Update");
        public void OnExit() => Console.WriteLine("[WalkState] Exit");
    }

    public partial class RunState(Player parent) : LazyStateBase<Player>(parent)
    {
        public void OnEnter() => Console.WriteLine("[RunState] Enter");
        public void OnUpdate() => Console.WriteLine("[RunState] Update");
        public void OnExit() => Console.WriteLine("[RunState] Exit");
    }

    public class Player
    {
        public enum State
        {
            Idle,
            Walk,
            Run,
        }

        private readonly LazyStateMachine<Player, State> sm = new();

        private static void PrintMemoryUsage(string label)
        {
            long memoryBytes = GC.GetTotalMemory(true);
            double memoryMB = memoryBytes / (1024.0 * 1024.0);
            Console.WriteLine($"{label}: {memoryMB:F3} MB");
        }

        public Player()
        {
            Console.WriteLine("=== LazyStateMachine Tester ===");

            // メモリ使用量記録
            PrintMemoryUsage("Before StateMachine creation");

            // ステートマシンを作成
            PrintMemoryUsage("After StateMachine creation");

            // 状態を登録
            sm.RegisterState(State.Idle, new IdleState(this));
            sm.RegisterState(State.Walk, new WalkState(this));
            sm.RegisterState(State.Run, new RunState(this));

            PrintMemoryUsage("After state registration");

            // 初期化
            sm.Initialize();
            PrintMemoryUsage("After initialization");

            // 更新テスト
            sm.Update();
            PrintMemoryUsage("After first update");

            // 状態遷移テスト
            ChangeState(State.Walk);
            sm.Update();

            ChangeState(State.Run);
            sm.Update();

            ChangeState(State.Idle);
            sm.Update();

            // 未登録の状態を試す（エラーハンドリング確認）
            ChangeState((State)999);

            // メモリ使用量の最終確認
            PrintMemoryUsage("Final memory usage");
        }

        private void ChangeState(State newState)
        {
            Console.WriteLine($"--- Changing State: {newState} ---");
            sm.ChangeState(newState);
            PrintMemoryUsage($"After changing to {newState}");
        }
    }

    public static class Program
    {
        public static void Main()
        {
            _ = new Player();

            // プロセス全体のメモリ使用量を表示
            double processMemoryMB = Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0);
            Console.WriteLine($"Total Process Memory Usage: {processMemoryMB:F3} MB");
        }
    }
}

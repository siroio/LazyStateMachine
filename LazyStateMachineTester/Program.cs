using System;
using System.Diagnostics;
using LazyStateMachine;

namespace LazyStateMachineTester
{
    public partial class IdleState : LazyStateBase
    {
        public void OnEnter() => Console.WriteLine("IdleState: Enter");
        public void OnUpdate() => Console.WriteLine("IdleState: Update");
        public void OnExit() => Console.WriteLine("IdleState: Exit");
    }

    public partial class WalkState : LazyStateBase
    {
        public void OnEnter() => Console.WriteLine("WalkState: Enter");
        public void OnUpdate() => Console.WriteLine("WalkState: Update");
        public void OnExit() => Console.WriteLine("WalkState: Exit");
    }

    public class Player
    {
        public enum State
        {
            Idle = 0,
            Walk,
        }

        private static void PrintMemoryUsage(string label)
        {
            long memoryBytes = GC.GetTotalMemory(true);
            double memoryMB = memoryBytes / (1024.0 * 1024.0);
            Console.WriteLine($"{label}: {memoryMB:F3} MB");
        }

        public Player()
        {
            PrintMemoryUsage("Memory before StateMachine creation");

            var sm = new LazyStateMachine<Player, State>();

            PrintMemoryUsage("Memory after StateMachine creation");

            sm.RegisterState(State.Idle, new IdleState());
            sm.RegisterState(State.Walk, new WalkState());

            PrintMemoryUsage("Memory after state registration");

            sm.Initialize(this);
            PrintMemoryUsage("Memory after initialization");

            sm.Update();
            PrintMemoryUsage("Memory after first update");

            sm.ChangeState(State.Walk);
            PrintMemoryUsage("Memory after changing to WalkState");

            sm.Update();
            PrintMemoryUsage("Memory after second update");

            sm.ChangeState(State.Idle);
            PrintMemoryUsage("Memory after changing back to IdleState");
        }
    }

    public static class Program
    {
        public static void Main()
        {
            _ = new Player();

            // プロセス全体のメモリ使用量も表示
            double processMemoryMB = Process.GetCurrentProcess().WorkingSet64 / (1024.0 * 1024.0);
            Console.WriteLine($"Total Process Memory Usage: {processMemoryMB:F3} MB");
        }
    }
}

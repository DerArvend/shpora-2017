using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RobotTask
{
    public class Robot : IRobot
    {
        private readonly Dictionary<string, Action<string[]>> methods;
        private ListStack<string> stack;
        private Dictionary<string, int> labelsIndex;
        private List<Command> parsedCommandsList;

        private int index;

        private Func<string> readInput;
        private Action<string> writeOutput;

        #region init

        public Robot()
        {
            methods = new Dictionary<string, Action<string[]>>
            {
                {"PUSH", Push},
                {"POP", Pop},
                {"READ", Read},
                {"WRITE", Write},
                {"SWAP", Swap},
                {"COPY", Copy},
                {"JMP", Jmp},
                {"CONCAT", Concat},
                {"REPLACEONE", ReplaceOne},
            };
        }


        public List<string> Evaluate(List<string> commands, IEnumerable<string> input)
        {
            var inputEnumerator = input.GetEnumerator();
            var outputList = new List<string>();

            readInput = inputEnumerator.GetNext;
            writeOutput = outputList.Add;

            Run(commands);
            return outputList;
        }

        public void Evaluate(List<string> commands)
        {
            readInput = Console.ReadLine;
            writeOutput = Console.WriteLine;
            Run(commands);
        }

        private void InitializeFields(List<string> commands)
        {
            stack = new ListStack<string>();
            labelsIndex = Enumerable
                .Range(0, commands.Count)
                .Where(i => !string.IsNullOrEmpty(commands[i]))
                .Where(i => commands[i].StartsWith("LABEL"))
                .ToDictionary(i => commands[i].Split()[1], i => i);

            ParseCommands(commands);
        }

        private void ParseCommands(List<string> commands)
        {
            parsedCommandsList = new List<Command>();
            foreach (var cmd in commands)
            {
                var splittedCommand = cmd.Split();
                if (string.IsNullOrEmpty(cmd) || !methods.ContainsKey(splittedCommand[0]))
                    parsedCommandsList.Add(null);

                else
                {
                    var action = methods[splittedCommand[0]];
                    var args = splittedCommand.Skip(1).ToArray();
                    parsedCommandsList.Add(new Command(action, args));
                }
            }
        }

        #endregion

        private void Run(List<string> commands)
        {
            InitializeFields(commands);
            for (index = 0; index < parsedCommandsList.Count; index++)
            {
                if (parsedCommandsList[index] != null)
                    parsedCommandsList[index].Execute();
            }
        }


        private void Push(string[] args)
        {
            var stringToPush = String.Join(" ", args);
            stack.Push(stringToPush.Substring(1, stringToPush.Length - 2).Replace("''", "'"));
        }

        private void Pop(string[] args) => stack.Pop();
        private void Read(string[] args) => stack.Push(readInput());
        private void Write(string[] args) => writeOutput(stack.Peek());

        private void Swap(string[] args)
        {
            var index1 = int.Parse(args[0]) - 1;
            var index2 = int.Parse(args[1]) - 1;

            var temp = stack[index1];
            stack[index1] = stack[index2];
            stack[index2] = temp;
        }

        private void Copy(string[] args)
        {
            var index = int.Parse(args[0]) - 1;
            stack.Push(stack[index]);
        }

        private void Jmp(string[] args)
        {
            var mark = args.Length > 0 && !string.IsNullOrEmpty(args[0]) ? args[0] : stack.Pop();
            Jmp(mark);
        }

        private void Jmp(string mark)
        {
            index = labelsIndex[mark];
        }

        private void Concat(string[] args)
        {
            stack.Push(stack.Pop() + stack.Pop());
        }

        private void ReplaceOne(string[] args)
        {
            var stringToChange = stack.Pop();
            var substringToRemove = stack.Pop();
            var substringToInsert = stack.Pop();
            var mark = stack.Pop();

            var regex = new Regex(substringToRemove);
            if (regex.IsMatch(stringToChange))
                stack.Push(regex.Replace(stringToChange, substringToInsert, 1));

            else
            {
                stack.Push(stringToChange);
                Jmp(mark);
            }
        }
    }

    public class ListStack<T> : IEnumerable<T>
    {
        private readonly List<T> list;
        public int Count => list.Count;

        public ListStack()
        {
            list = new List<T>();
        }

        public T Peek()
        {
            if (list.Count == 0)
                throw new InvalidOperationException();

            return list[list.Count - 1];
        }

        public T Pop()
        {
            var result = Peek();
            list.RemoveAt(list.Count - 1);
            return result;
        }

        public void Push(T value) => list.Add(value);

        public T this[int index]
        {
            get => list[list.Count - index - 1];
            set => list[list.Count - index - 1] = value;
        }

        public IEnumerator<T> GetEnumerator()
        {
            var currentIndex = list.Count - 1;
            while (currentIndex >= 0)
            {
                yield return list[currentIndex];
                currentIndex--;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    public static class EnumeratorExtensions
    {
        public static T GetNext<T>(this IEnumerator<T> enumerator)
        {
            if (enumerator.MoveNext())
                return enumerator.Current;

            throw new IndexOutOfRangeException();
        }
    }

    public class Command
    {
        private readonly Action<string[]> action;
        private readonly string[] args;

        public Command(Action<string[]> action, string[] args)
        {
            this.action = action;
            this.args = args;
        }

        public void Execute() => action.Invoke(args);
    }
}

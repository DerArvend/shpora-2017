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

        private IEnumerator<string> inputEnumerator;
        private List<String> outputList;

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
            InitializeFields(commands);
            outputList = new List<string>();
            inputEnumerator = input.GetEnumerator();
            readInput = () =>
            {
                if (inputEnumerator.MoveNext())
                    return inputEnumerator.Current;
                return null;
            };
            writeOutput = outputList.Add;
            Run();
            return outputList;
        }

        public void Evaluate(List<string> commands)
        {
            InitializeFields(commands);
            readInput = Console.ReadLine;
            writeOutput = Console.WriteLine;
            Run();
        }

        private void InitializeFields(List<string> commands)
        {
            stack = new ListStack<string>();
            parsedCommandsList = new List<Command>();

            labelsIndex = Enumerable
                .Range(0, commands.Count)
                .Where(i => commands[i].StartsWith("LABEL"))
                .ToDictionary(i => commands[i].Split()[1], i => i);

            ParseCommands(commands);
        }

        private void ParseCommands(List<string> commands)
        {
            foreach (var cmd in commands)
            {
                var splittedCommand = cmd.Split();
                if (string.IsNullOrEmpty(cmd) || !methods.ContainsKey(splittedCommand[0]))
                    parsedCommandsList.Add(null);

                else
                    parsedCommandsList.Add(new Command(
                        methods[splittedCommand[0]],
                        splittedCommand.Skip(1).ToArray())
                    );
            }
        }

        #endregion

        private void Run()
        {
            for (index = 0; index < parsedCommandsList.Count; index++)
            {
                if (parsedCommandsList[index] == null)
                    continue;

                parsedCommandsList[index].Run();
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
            var stringToReplace = stack.Pop();
            var stringToInsert = stack.Pop();
            var mark = stack.Pop();

            var regex = new Regex(stringToReplace);
            if (regex.IsMatch(stringToChange))
                stack.Push(regex.Replace(stringToChange, stringToInsert, 1));
            else
            {
                stack.Push(stringToChange);
                Jmp(mark);
            }
        }
    }

    public class ListStack<T> : IEnumerable<T>
    {
        private List<T> list;
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

    public class Command
    {
        private Action<string[]> method;
        private string[] args;

        public Command(Action<string[]> method, string[] args)
        {
            this.method = method;
            this.args = args;
        }

        public void Run() => method.Invoke(args);
    }
}

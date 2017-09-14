using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RobotTask
{
    public class Robot : IRobot
    {
        private readonly Dictionary<string, Action<object[]>> methods;
        private ListStack<string> stack;
        private List<string> outputList;
        private Dictionary<string, int> labelsIndex;

        private List<Action<object[]>> parsedCommands;
        private List<object[]> args;

        private int index;

        private Func<string> readInput;
        private Action<string> writeOutput;

        public Robot()
        {
            methods = new Dictionary<string, Action<object[]>>
            {
                {"PUSH", Push},
                {"POP", Pop},
                {"READ", Read},
                {"WRITE", Write},
                {"SWAP", Swap},
                {"COPY", Copy},
                {"JMP", Jmp},
                {"CONCAT", Concat},
                {"REPLACEONE", Replaceone},
            };
        }


        #region evaluate

        public List<string> Evaluate(List<string> commands, IEnumerable<string> input)
        {
            InitializeFields(commands);
            var enumerator = input.GetEnumerator();
            readInput = () =>
            {
                enumerator.MoveNext();
                return enumerator.Current;
            };
            outputList = new List<string>();
            writeOutput = outputList.Add;
            Run(commands);
            return outputList;
        }

        public void Evaluate(List<string> commands)
        {
            InitializeFields(commands);
            readInput = Console.ReadLine;
            writeOutput = Console.WriteLine;
            Run(commands);
        }

        #endregion

        private void InitializeFields(List<string> commands)
        {
            stack = new ListStack<string>();
            outputList = new List<string>();
            parsedCommands = new List<Action<object[]>>();
            args = new List<object[]>();
            labelsIndex = Enumerable.Range(0, commands.Count)
                .Where(i => commands[i].StartsWith("LABEL"))
                .ToDictionary(i => commands[i].Split()[1], i => i);
        }

        private void ParseCommands(List<string> commands)
        {
            for (int i = 0; i < commands.Count; i++)
            {
                if (String.IsNullOrEmpty(commands[i]))
                {
                    parsedCommands.Add(null);
                    this.args.Add(null);
                    continue;
                }
                ;

                var splittedCommdand = commands[i].Split();
                var command = splittedCommdand[0];
                var args = splittedCommdand.Skip(1).ToArray();

                if (command == "LABEL")
                {
                    parsedCommands.Add(null);
                    this.args.Add(null);
                    continue;
                }

                parsedCommands.Add(methods[command]);
                if (command == "SWAP" || command == "COPY")
                    this.args.Add(args.Select(x => int.Parse(x)).Cast<object>().ToArray());
                else
                {
                    this.args.Add(args.Cast<object>().ToArray());
                }
            }
        }

        private void Run(List<string> commands)
        {
            ParseCommands(commands);
            for (index = 0; index < commands.Count; index++)
            {
                if (parsedCommands[index] != null)
                    parsedCommands[index].Invoke(args[index]);
            }
        }

        private void Jmp(object[] args)
        {
            if (args.Length != 0)
                Jump((string) args[0]);
            else
                Jump(stack.Pop());
        }

        private void Jump(string mark)
        {
            if (string.IsNullOrEmpty(mark))
                mark = stack.Pop();
            index = labelsIndex[mark];
        }

        private void Push(object[] args)
        {
            stack.Push(Regex.Replace((string) args[0], @"\'(.|$)", "$1"));
        }

        private void Pop(object[] args) => stack.Pop();
        private void Read(object[] args) => stack.Push(readInput());
        private void Write(object[] args) => writeOutput(stack.Peek());

        private void Swap(object[] args)
        {
            var firstIndex = stack.Count - (int) args[0];
            var secondIndex = stack.Count - (int) args[1];


            var temp = stack.GetElement(firstIndex);
            stack.SetElement(stack.GetElement(secondIndex), firstIndex);
            stack.SetElement(temp, secondIndex);
        }


        private void Copy(object[] args)
        {
            stack.Push(
                stack.GetElement(stack.Count - (int) args[0])
            );
        }

        private void Concat(object[] args)
        {
            stack.Push(stack.Pop() + stack.Pop());
        }

        private void Replaceone(object[] args)
        {
            var stringToChange = stack.Pop();
            var substringToRemove = stack.Pop();
            var substringToInsert = stack.Pop();
            var mark = stack.Pop();
            var i = stringToChange.IndexOf(substringToRemove);


            if (i == -1)
            {
                stack.Push(stringToChange);
                Jump(mark);
                return;
            }

            stringToChange = stringToChange
                .Remove(i, substringToRemove.Length)
                .Insert(i, substringToInsert);
            stack.Push(stringToChange);
        }
    }

    public class ListStack<T>
    {
        private List<T> list = new List<T>();
        public int Count => list.Count;

        public void Push(T value)
        {
            list.Add(value);
        }

        public T Pop()
        {
            if (list.Count == 0)
                throw new InvalidOperationException();

            var result = list[list.Count - 1];
            list.RemoveAt(list.Count - 1);
            return result;
        }

        public T Peek()
        {
            if (list.Count == 0)
                throw new InvalidOperationException();

            return list[list.Count - 1];
        }

        public T GetElement(int index) => list[index];
        public void SetElement(T value, int index) => list[index] = value;
    }
}
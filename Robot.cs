using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace RobotTask
{
    public class Robot : IRobot
    {
        private readonly Dictionary<string, Action<string>> methods;
        private ListStack<string> stack;
        private List<string> outputList;
        private Dictionary<string, int> labelsIndex;

        private int index;

        private Func<string> readInput;
        private Action<string> writeOutput;

        public Robot()
        {
            methods = new Dictionary<string, Action<string>>
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
            labelsIndex = Enumerable.Range(0, commands.Count)
                .Where(i => commands[i].StartsWith("LABEL"))
                .ToDictionary(i => commands[i].Split()[1], i => i);
        }

        private void Run(List<string> commands)
        {
            for (index = 0; index < commands.Count; index++)
            {
                if (String.IsNullOrEmpty(commands[index])) continue;

                var splittedCommdand = commands[index].Split(new[] {' '}, 2);
                var command = splittedCommdand[0];
                var args = (splittedCommdand.Length > 1) ? splittedCommdand[1] : "";

                if (methods.ContainsKey(command))
                    methods[command](args);
            }
        }

        private void Jmp(string args)
        {
            var mark = string.IsNullOrEmpty(args) ? stack.Pop() : args;
            index = labelsIndex[mark];
        }

        private void Push(string args)
        {
            stack.Push(Regex.Replace(args, @"\'(.|$)", "$1"));
        }

        private void Pop(string args) => stack.Pop();
        private void Read(string args) => stack.Push(readInput());
        private void Write(string args) => writeOutput(stack.Peek());

        private void Swap(string args)
        {
            var indexes = args
                .Split()
                .Select(x => stack.Count - int.Parse(x))
                .ToArray();

            var temp = stack.GetElement(indexes[0]);
            stack.SetElement(stack.GetElement(indexes[1]), indexes[0]);
            stack.SetElement(temp, indexes[1]);
        }

        private void Copy(string args)
        {
            stack.Push(
                stack.GetElement(stack.Count - int.Parse(args))
            );
        }

        private void Concat(string args)
        {
            stack.Push(stack.Pop() + stack.Pop());
        }

        private void Replaceone(string args)
        {
            var stringToChange = stack.Pop();
            var substringToRemove = stack.Pop();
            var substringToInsert = stack.Pop();
            var mark = stack.Pop();

            var i = stringToChange.IndexOf(substringToRemove);

            if (i == -1)
            {
                stack.Push(stringToChange);
                Jmp(mark);
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

        public void SetElement(T value, int index)
        {
            list[index] = value;
        }
    }
}
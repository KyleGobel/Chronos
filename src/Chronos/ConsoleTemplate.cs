using System;
using System.IO;

namespace Chronos
{
    public abstract class ConsoleProcessTemplateBase<TOptions> where TOptions: new()
    {

        protected TextWriter Error;
        protected TextReader Input;
        protected TextWriter Output;

        protected  TOptions Options;

        public void Process(string[] args, TextReader input, TextWriter output, TextWriter error, Action<string[], TOptions> parseOptions)
        {
            Error = error;
            Output = output;
            Input = input;

            ParseOptions(args, parseOptions);

            var isValidArguments = ValidateArguments();

            if (isValidArguments)
            {
                PreProcess();
                ProcessLines();
                PostProcess();
            }
        }

        private void ParseOptions(string[] args, Action<string[],TOptions> parseOptions)
        {
            Options = new TOptions();

            parseOptions(args, Options);
        }

        protected virtual bool ValidateArguments()
        {
            return true;
        }

        private void ProcessLines()
        {
            var currentLine = Input.ReadLine();

            while (currentLine != null)
            {
                ProcessLine(currentLine);

                currentLine = Input.ReadLine();
            }
        }

        protected virtual void ProcessLine(string line) {}

        protected virtual void PostProcess() {}

        protected virtual void PreProcess() {}
    }
}
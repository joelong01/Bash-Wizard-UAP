using System;
using System.Collections.Generic;
using System.Text;

namespace bashWizardShared
{
    public enum ErrorLevel { Information, Warning, Fatal, Validation};

    public class ParseErrorInfo
    {
        public ErrorLevel ErrorLevel { get; set; } = ErrorLevel.Information;
        public string Message { get; set; } = "";
        public object Tag { get; set; } = null;
        public ParseErrorInfo(ErrorLevel level, string message)
        {
            ErrorLevel = level;
            Message = message;
        }
        public override string ToString()
        {
            return $"{ErrorLevel}-{Message}\n";
        }
    }
}

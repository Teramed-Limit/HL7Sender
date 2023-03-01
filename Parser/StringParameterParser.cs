using HL7Sender.Function;
using SendingPDFHL7;

namespace HL7Sender.Parser
{
    public static class StringParameterParser
    {
        public static string Parse(string paramsStr)
        {
            var restStr = paramsStr;
            var finalResult = paramsStr;

            // 解析{}內的自串
            while (restStr.Length != 0)
            {
                var keywordStart = restStr.IndexOf("{", StringComparison.Ordinal);
                if (keywordStart == -1) break;
                var keywordEnd = restStr.IndexOf("}", StringComparison.Ordinal);

                var parameterStr = restStr.Substring(keywordStart, keywordEnd - keywordStart + 1);

                var result = ParseFunctionStr(parameterStr.Replace("{", "").Replace("}", ""));
                restStr = restStr[(keywordEnd + 1)..];

                finalResult = finalResult.Replace(parameterStr, result);
            }

            return finalResult;
        }

        private static string ParseFunctionStr(string functionalStr)
        {
            // 解析()內的自串
            var keywordStart = functionalStr.IndexOf("(", StringComparison.Ordinal);
            if (keywordStart == -1)
                throw new Exception($"{functionalStr} is not a function");
            var keywordEnd = functionalStr.LastIndexOf(")", StringComparison.Ordinal);
            var functionName = functionalStr.Substring(0, keywordStart);
            var functionParameterStr = functionalStr
                .Substring(keywordStart, keywordEnd - keywordStart + 1)
                .Remove(keywordEnd - keywordStart, 1)
                .Remove(0, 1);

            var functionParameter = new string[functionParameterStr.Split(",").Length];
            var index = 0;
            foreach (var pramsStr in functionParameterStr.Split(","))
            {
                var isFunction = pramsStr.Contains('(') && pramsStr.EndsWith(")");
                if (isFunction)
                {
                    var parameter = ParseFunctionStr(pramsStr);
                    functionParameter[index] = parameter;
                }
                else
                {
                    functionParameter[index] = pramsStr;
                }

                index++;
            }

            return new FunctionFactory().Execute(functionName, functionParameter);
        }
    }
}
using System.Globalization;

namespace ActionOrbit.App.Services.MiniTools;

internal static class CalculatorEngine
{
    public static bool TryEvaluate(string? expression, out double result, out string issue)
    {
        result = 0;
        issue = "";
        if (string.IsNullOrWhiteSpace(expression))
        {
            issue = "Bir ifade yaz.";
            return false;
        }

        try
        {
            var parser = new Parser(expression);
            result = parser.Parse();
            if (!double.IsFinite(result))
            {
                throw new InvalidOperationException("Sonuç sonlu bir sayı değil.");
            }

            return true;
        }
        catch (InvalidOperationException ex)
        {
            issue = ex.Message;
            return false;
        }
    }

    public static string Format(double value) => value.ToString("G15", CultureInfo.InvariantCulture);

    private sealed class Parser
    {
        private readonly string _text;
        private int _position;

        public Parser(string expression) => _text = expression
            .Replace('×', '*')
            .Replace('÷', '/')
            .Replace('−', '-')
            .Replace(',', '.');

        public double Parse()
        {
            var result = ParseExpression();
            SkipWhitespace();
            if (_position != _text.Length)
            {
                throw Error("Beklenmeyen karakter");
            }

            return result;
        }

        private double ParseExpression()
        {
            var value = ParseTerm();
            while (true)
            {
                SkipWhitespace();
                if (Take('+'))
                {
                    value += ParseTerm();
                }
                else if (Take('-'))
                {
                    value -= ParseTerm();
                }
                else
                {
                    return value;
                }
            }
        }

        private double ParseTerm()
        {
            var value = ParseFactor();
            while (true)
            {
                SkipWhitespace();
                if (Take('*'))
                {
                    value *= ParseFactor();
                }
                else if (Take('/'))
                {
                    var divisor = ParseFactor();
                    if (Math.Abs(divisor) < double.Epsilon)
                    {
                        throw new InvalidOperationException("Sıfıra bölme yapılamaz.");
                    }

                    value /= divisor;
                }
                else if (Take('%'))
                {
                    var divisor = ParseFactor();
                    if (Math.Abs(divisor) < double.Epsilon)
                    {
                        throw new InvalidOperationException("Sıfıra göre kalan hesaplanamaz.");
                    }

                    value %= divisor;
                }
                else
                {
                    return value;
                }
            }
        }

        private double ParseFactor()
        {
            SkipWhitespace();
            if (Take('+'))
            {
                return ParseFactor();
            }

            if (Take('-'))
            {
                return -ParseFactor();
            }

            if (Take('('))
            {
                var value = ParseExpression();
                SkipWhitespace();
                if (!Take(')'))
                {
                    throw Error("Kapanış parantezi eksik");
                }

                return value;
            }

            return ParseNumber();
        }

        private double ParseNumber()
        {
            SkipWhitespace();
            var start = _position;
            var hasDecimalPoint = false;
            while (_position < _text.Length)
            {
                var current = _text[_position];
                if (char.IsDigit(current))
                {
                    _position++;
                    continue;
                }

                if (current == '.' && !hasDecimalPoint)
                {
                    hasDecimalPoint = true;
                    _position++;
                    continue;
                }

                break;
            }

            if (start == _position ||
                !double.TryParse(
                    _text[start.._position],
                    NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var value))
            {
                throw Error("Sayı bekleniyordu");
            }

            return value;
        }

        private void SkipWhitespace()
        {
            while (_position < _text.Length && char.IsWhiteSpace(_text[_position]))
            {
                _position++;
            }
        }

        private bool Take(char expected)
        {
            if (_position >= _text.Length || _text[_position] != expected)
            {
                return false;
            }

            _position++;
            return true;
        }

        private InvalidOperationException Error(string message) =>
            new($"{message} (konum {_position + 1}).");
    }
}

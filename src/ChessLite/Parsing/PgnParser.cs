using ChessLite.Movement;
using ChessLite.State;

namespace ChessLite.Parsing;

internal static class PgnParser
{
    internal static Game Parse(ReadOnlySpan<char> pgn)
    {
        var game = new Game();
        return Parse(pgn, game);
    }

    internal static Game Parse(ReadOnlySpan<char> pgn, Game game)
    {
        var parser = new Parser(pgn, game);
        return parser.Parse();
    }

    internal static Move MatchSanMove(Position position, ReadOnlySpan<char> san)
    {
        return SanParser.MatchMove(position, san);
    }

    private ref struct Parser
    {
        private readonly ReadOnlySpan<char> _pgn;
        private int _index;
        private Game _game;
        private bool _gameInitialized;

        internal Parser(ReadOnlySpan<char> pgn, Game game)
        {
            _pgn = pgn;
            _index = 0;
            _game = game;
            _gameInitialized = false;
        }

        internal Game Parse()
        {
            _game.ResetForParsing();
            ParseTags();
            if (!_gameInitialized)
            {
                InitializeFromFen(Constants.StartingPosition);
            }

            ParseMoveText();
            return _game;
        }

        private void ParseTags()
        {
            SkipWhitespace();
            while (_index < _pgn.Length && _pgn[_index] == '[')
            {
                ParseTag();
                SkipWhitespace();
            }
        }

        private void ParseTag()
        {
            _index++;
            var closeIndex = _pgn[_index..].IndexOf(']');
            if (closeIndex < 0)
            {
                throw new ArgumentException("Unterminated PGN tag.", nameof(_pgn));
            }

            var content = _pgn.Slice(_index, closeIndex).Trim();
            _index += closeIndex + 1;

            if (content.IsEmpty)
            {
                throw new ArgumentException("Invalid PGN tag.", nameof(_pgn));
            }

            var spaceIndex = content.IndexOf(' ');
            if (spaceIndex <= 0)
            {
                throw new ArgumentException("Invalid PGN tag.", nameof(_pgn));
            }

            var tagName = content[..spaceIndex];
            var valuePart = content[(spaceIndex + 1)..].Trim();
            if (valuePart.IsEmpty || valuePart[0] != '"')
            {
                throw new ArgumentException("Invalid PGN tag.", nameof(_pgn));
            }

            var lastQuoteIndex = valuePart.LastIndexOf('"');
            if (lastQuoteIndex <= 0)
            {
                throw new ArgumentException("Invalid PGN tag.", nameof(_pgn));
            }

            var trailing = valuePart[(lastQuoteIndex + 1)..].Trim();
            if (!trailing.IsEmpty)
            {
                throw new ArgumentException("Invalid PGN tag.", nameof(_pgn));
            }

            var value = valuePart.Slice(1, lastQuoteIndex - 1);
            if (tagName.Equals("Variant", StringComparison.Ordinal) || tagName.Equals("ECO", StringComparison.Ordinal))
            {
                throw new NotSupportedException($"PGN tag '{tagName.ToString()}' is not supported.");
            }

            if (tagName.Equals("FEN", StringComparison.Ordinal))
            {
                InitializeFromFen(value);
            }
        }

        private void InitializeFromFen(ReadOnlySpan<char> fen)
        {
            _game.Position.Reset();
            if (!FenParser.Parse(fen, _game.Position))
            {
                throw new ArgumentException("Invalid FEN string", nameof(_pgn));
            }

            _gameInitialized = true;
        }

        private void ParseMoveText()
        {
            while (_index < _pgn.Length)
            {
                SkipWhitespace();
                if (_index >= _pgn.Length)
                {
                    return;
                }

                var current = _pgn[_index];
                if (current == '{')
                {
                    SkipBraceComment();
                    continue;
                }

                if (current == ';')
                {
                    SkipLineComment();
                    continue;
                }

                if (current == '(')
                {
                    SkipVariation();
                    continue;
                }

                if (current == '$')
                {
                    SkipNag();
                    continue;
                }

                if (current == '[')
                {
                    throw new ArgumentException("Unexpected tag in movetext.", nameof(_pgn));
                }

                var token = ReadToken();
                if (token.IsEmpty)
                {
                    continue;
                }

                if (IsResultToken(token))
                {
                    return;
                }

                token = StripMoveNumber(token);
                if (token.IsEmpty)
                {
                    continue;
                }

                token = SanParser.TrimAnnotations(token);
                if (token.IsEmpty)
                {
                    continue;
                }

                var move = PgnParser.MatchSanMove(_game.Position, token);
                _game.MakeMove(move);
            }
        }

        private ReadOnlySpan<char> ReadToken()
        {
            var start = _index;
            while (_index < _pgn.Length)
            {
                var current = _pgn[_index];
                if (char.IsWhiteSpace(current) || current == '{' || current == '(' || current == ';')
                {
                    break;
                }

                if (current == ')')
                {
                    throw new ArgumentException("Unexpected variation end.", nameof(_pgn));
                }

                if (current == '[')
                {
                    throw new ArgumentException("Unexpected tag in movetext.", nameof(_pgn));
                }

                _index++;
            }

            return _pgn[start.._index];
        }

        private void SkipWhitespace()
        {
            while (_index < _pgn.Length && char.IsWhiteSpace(_pgn[_index]))
            {
                _index++;
            }
        }

        private void SkipBraceComment()
        {
            _index++;
            var closeIndex = _pgn[_index..].IndexOf('}');
            if (closeIndex < 0)
            {
                throw new ArgumentException("Unterminated PGN comment.", nameof(_pgn));
            }

            _index += closeIndex + 1;
        }

        private void SkipLineComment()
        {
            _index++;
            while (_index < _pgn.Length)
            {
                var current = _pgn[_index];
                if (current == '\n')
                {
                    _index++;
                    return;
                }

                if (current == '\r')
                {
                    _index++;
                    if (_index < _pgn.Length && _pgn[_index] == '\n')
                    {
                        _index++;
                    }
                    return;
                }

                _index++;
            }
        }

        private void SkipVariation()
        {
            var depth = 0;
            while (_index < _pgn.Length)
            {
                var current = _pgn[_index];
                if (current == '(')
                {
                    depth++;
                    _index++;
                    continue;
                }

                if (current == ')')
                {
                    depth--;
                    _index++;
                    if (depth == 0)
                    {
                        return;
                    }
                    continue;
                }

                if (current == '{')
                {
                    SkipBraceComment();
                    continue;
                }

                if (current == ';')
                {
                    SkipLineComment();
                    continue;
                }

                _index++;
            }

            throw new ArgumentException("Unterminated PGN variation.", nameof(_pgn));
        }

        private void SkipNag()
        {
            _index++;
            while (_index < _pgn.Length && char.IsDigit(_pgn[_index]))
            {
                _index++;
            }
        }

        private static bool IsResultToken(ReadOnlySpan<char> token)
        {
            return token.Equals("1-0", StringComparison.Ordinal) ||
                   token.Equals("0-1", StringComparison.Ordinal) ||
                   token.Equals("1/2-1/2", StringComparison.Ordinal) ||
                   token.Equals("*", StringComparison.Ordinal);
        }

        private static ReadOnlySpan<char> StripMoveNumber(ReadOnlySpan<char> token)
        {
            var index = 0;
            while (index < token.Length && char.IsDigit(token[index]))
            {
                index++;
            }

            if (index == 0)
            {
                return token;
            }

            var dotIndex = index;
            while (dotIndex < token.Length && token[dotIndex] == '.')
            {
                dotIndex++;
            }

            if (dotIndex == index)
            {
                return token;
            }

            return token[dotIndex..];
        }

    }
}

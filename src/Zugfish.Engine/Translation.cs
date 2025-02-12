namespace Zugfish.Engine;

public static class Translation
{
    public static int SquareFromUci(char file, char rank)
    {
        if (file < 'a' || file > 'h' || rank < '1' || rank > '8')
            throw new ArgumentException("Invalid UCI square.", nameof(file));

        return (rank - '1') * 8 + (file - 'a');
    }

    public static int SquareFromUci(ReadOnlySpan<char> square)
    {
        if (square.Length != 2) throw new ArgumentException("Invalid square", nameof(square));
        var file = square[0];
        var rank = square[1];

        if (file < 'a' || file > 'h' || rank < '1' || rank > '8')
            throw new ArgumentException("Invalid UCI square.", nameof(file));

        return (rank - '1') * 8 + (file - 'a');
    }
}

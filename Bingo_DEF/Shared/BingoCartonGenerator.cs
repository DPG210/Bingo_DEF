namespace BingoTop;

public static class BingoCartonGenerator
{
    public static List<int?[,]> GenerateCartones(int cartonCount, IEnumerable<FavoriteChoice> favoritos, Random? random = null)
    {
        random ??= Random.Shared;
        var favorites = favoritos
            .Where(f => f is not null && f.Numero >= 1 && f.Numero <= 90)
            .Select(f => new FavoriteChoice
            {
                Numero = f.Numero,
                Cartones = f.Cartones.Where(c => c >= 1 && c <= cartonCount).Distinct().ToList()
            })
            .Where(f => f.Cartones.Count > 0)
            .ToList();

        var cartones = new List<int?[,]>();
        for (int i = 1; i <= cartonCount; i++)
        {
            var forced = favorites
                .Where(f => f.Cartones.Contains(i))
                .Select(f => f.Numero)
                .Distinct()
                .ToList();

            cartones.Add(GenerateCarton(forced, random));
        }

        return cartones;
    }

    private static int?[,] GenerateCarton(IReadOnlyCollection<int> forcedNumbers, Random random)
    {
        var forced = forcedNumbers
            .Where(n => n is >= 1 and <= 90)
            .Distinct()
            .ToList();

        for (int attempt = 0; attempt < 500; attempt++)
        {
            var occupied = new bool[3, 9];
            var rowCounts = new int[3];
            var colCounts = new int[9];
            var forcedByColumn = Enumerable.Range(0, 9).ToDictionary(c => c, _ => new List<int>());

            var valid = true;
            foreach (var number in forced)
            {
                var column = GetColumn(number);
                var rows = Enumerable.Range(0, 3)
                    .Where(r => rowCounts[r] < 5 && !occupied[r, column])
                    .ToList();

                if (rows.Count == 0)
                {
                    valid = false;
                    break;
                }

                var row = rows[random.Next(rows.Count)];
                occupied[row, column] = true;
                rowCounts[row]++;
                colCounts[column]++;
                forcedByColumn[column].Add(number);
            }

            if (!valid)
                continue;

            for (int row = 0; row < 3 && valid; row++)
            {
                while (rowCounts[row] < 5)
                {
                    var candidates = Enumerable.Range(0, 9)
                        .Where(c => !occupied[row, c] && colCounts[c] < 3)
                        .ToList();

                    if (candidates.Count == 0)
                    {
                        valid = false;
                        break;
                    }

                    var column = candidates[random.Next(candidates.Count)];
                    occupied[row, column] = true;
                    rowCounts[row]++;
                    colCounts[column]++;
                }
            }

            if (!valid)
                continue;

            var matrix = new int?[3, 9];
            for (int column = 0; column < 9; column++)
            {
                var rows = Enumerable.Range(0, 3).Where(r => occupied[r, column]).ToList();
                if (rows.Count == 0)
                    continue;

                var pool = GetColumnPool(column)
                    .Where(n => !forced.Contains(n))
                    .OrderBy(_ => random.Next())
                    .ToList();

                var values = forcedByColumn[column]
                    .Concat(pool.Take(rows.Count - forcedByColumn[column].Count))
                    .OrderBy(n => n)
                    .ToList();

                if (values.Count != rows.Count)
                {
                    valid = false;
                    break;
                }

                rows.Sort();
                for (int i = 0; i < rows.Count; i++)
                    matrix[rows[i], column] = values[i];
            }

            if (!valid)
                continue;

            return matrix;
        }

        throw new InvalidOperationException("No se pudo generar el cartón.");
    }

    private static int GetColumn(int number)
    {
        if (number <= 9) return 0;
        if (number <= 19) return 1;
        if (number <= 29) return 2;
        if (number <= 39) return 3;
        if (number <= 49) return 4;
        if (number <= 59) return 5;
        if (number <= 69) return 6;
        if (number <= 79) return 7;
        return 8;
    }

    private static IEnumerable<int> GetColumnPool(int column)
    {
        int min = column == 0 ? 1 : column * 10;
        int max = column == 8 ? 90 : (column * 10) + 9;
        for (int n = min; n <= max; n++)
            yield return n;
    }
}

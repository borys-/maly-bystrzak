using MalyBystrzak.Core;

namespace MalyBystrzak.Modules.Educational;

public sealed record ProductGridPuzzle(int Number, int Tier, int[] Values, int[] IconKinds, int[] Cells,
    int[] RowProducts, int[] ColumnProducts, CognitiveDifficulty Difficulty)
{
    public int CountSolutions()
    {
        var count = 0;
        for (var a = 1; a <= 9; a++) for (var b = 1; b <= 9; b++) for (var c = 1; c <= 9; c++)
        for (var d = 1; d <= 9; d++) for (var f = 1; f <= 9; f++)
        {
            var values = new[] { a, b, c, d, f };
            if (Enumerable.Range(0, 3).All(row => Enumerable.Range(0, 3)
                    .Select(column => values[Cells[row * 3 + column]]).Aggregate(1, (x, y) => x * y) == RowProducts[row]) &&
                Enumerable.Range(0, 3).All(column => Enumerable.Range(0, 3)
                    .Select(row => values[Cells[row * 3 + column]]).Aggregate(1, (x, y) => x * y) == ColumnProducts[column]))
                count++;
            if (count > 1) return count;
        }
        return count;
    }
}

public sealed class ProductGridModule : IWorksheetModule
{
    private static readonly WorksheetInstruction Rules = new("Tabela iloczynów",
        "Odkryj liczby ukryte pod obrazkami.", "Iloczyny wierszy i kolumn są podane.", "#f39a3c");
    public string Id => "product-grid";
    public string DisplayName => "Tabela iloczynów";
    public string Symbol => "▦";
    public WorksheetInstruction Instruction => Rules;
    public IReadOnlyList<WorksheetVariant> Variants { get; } =
        [new("3x3", "Tabela iloczynów", "Obrazkowa tabliczka mnożenia 3 × 3", WorksheetLayout.FullPage)];
    public IReadOnlyList<string> Validate(ModuleGenerationRequest request) => request.Count <= 0
        ? ["Liczba zadań musi być większa od zera."]
        : request.VariantId != "3x3" ? ["Nieobsługiwany wariant tabeli iloczynów."] : [];

    public IReadOnlyList<GeneratedWorksheet> Generate(ModuleGenerationRequest request,
        IProgress<GenerationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var errors = Validate(request); if (errors.Count > 0) throw new ArgumentException(string.Join(' ', errors));
        var random = new Random(request.Seed); var result = new List<GeneratedWorksheet>();
        for (var index = 0; index < request.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var tier = PuzzleSupport.Tier(index, request.Count);
            var values = Enumerable.Range(1, tier <= 2 ? 5 : 8).OrderBy(_ => random.Next()).Take(5).ToArray();
            var cells = new[] { 0, 0, 0, 1, 1, 1, 2, 3, 4 };
            var rows = Enumerable.Range(0, 3).Select(row => Enumerable.Range(0, 3)
                .Select(column => values[cells[row * 3 + column]]).Aggregate(1, (a, b) => a * b)).ToArray();
            var columns = Enumerable.Range(0, 3).Select(column => Enumerable.Range(0, 3)
                .Select(row => values[cells[row * 3 + column]]).Aggregate(1, (a, b) => a * b)).ToArray();
            var puzzle = new ProductGridPuzzle(index + 1, tier, values,
                Enumerable.Range(0, 5).OrderBy(_ => random.Next()).ToArray(), cells, rows, columns,
                PuzzleSupport.Difficulty(tier, 60 + tier * 5, 62 + tier * 5, 60));
            result.Add(ToWorksheet(puzzle));
            progress?.Report(new(index + 1, request.Count, $"Tabela iloczynów: {index + 1}/{request.Count}"));
        }
        return result;
    }

    private static GeneratedWorksheet ToWorksheet(ProductGridPuzzle puzzle) => new(puzzle.Number,
        "product-grid", "3x3", "Tabela iloczynów",
        PuzzleSupport.Fingerprint(puzzle.Number, string.Join(',', puzzle.Values), string.Join(',', puzzle.Cells)), puzzle.Difficulty,
        puzzle.Difficulty.Stars, Visual(puzzle, false), Visual(puzzle, true), Rules, WorksheetLayout.FullPage);

    private static WorksheetVisual Visual(ProductGridPuzzle puzzle, bool solution)
    {
        var e = new List<VisualElement>(); const double cell = 18; const double x0 = 14; const double y0 = 8;
        for (var row = 0; row < 3; row++) for (var column = 0; column < 3; column++)
        {
            var x = x0 + column * cell; var y = y0 + row * cell;
            e.Add(new VisualRectangle(x, y, cell, cell, "#fffdf8", "#c8c8d8", .7));
            var icon = puzzle.Cells[row * 3 + column];
            PuzzleSupport.Icon(e, puzzle.IconKinds[icon], x + cell / 2, y + cell / 2, .62);
            if (solution) e.Add(PuzzleSupport.Text(x + cell - 2, y + cell - 2, puzzle.Values[icon].ToString(), 3.5, true, "#19a88e"));
        }
        for (var row = 0; row < 3; row++) e.Add(PuzzleSupport.Text(76, y0 + row * cell + 11, puzzle.RowProducts[row].ToString(), 6));
        for (var column = 0; column < 3; column++) e.Add(PuzzleSupport.Text(x0 + column * cell + 9, 72, puzzle.ColumnProducts[column].ToString(), 6));
        for (var i = 0; i < 5; i++)
        {
            PuzzleSupport.Icon(e, puzzle.IconKinds[i], 12 + i * 21, 88, .48);
            PuzzleSupport.AnswerBox(e, 6 + i * 21, 97, 12, 11, solution ? puzzle.Values[i].ToString() : null);
        }
        return new(108, 112, e);
    }
}

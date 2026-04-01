
using System.Security.Cryptography;
using Krizaljka.Domain.Template;

namespace Krizaljka.Domain.TemplateAnalysis;

public class KrizaljkaAnalyzer
{
    private static readonly HashSet<KrizaljkaCellType> InputCells =
    [
        KrizaljkaCellType.Input, KrizaljkaCellType.InputColon, KrizaljkaCellType.InputDash
    ];

    private int _lastSlotId = 1;

    public KrizaljkaTemplateAnalysis GeTemplateAnalysis(KrizaljkaTemplate template)
    {
        if (template.Rows.Length == 0)
        {
            return new KrizaljkaTemplateAnalysis(template.Id, template, [], []);
        }

        List<KrizaljkaSlot> slots = [];

        var rows = template.Rows;
        for (var r = 0; r < rows.Length; r++)
        {   
            for (var c = 0; c < rows[r].Length; c++)
            {
                var currentCell = (KrizaljkaCellType)rows[r][c];

                if (currentCell is KrizaljkaCellType.DescRight or
                    KrizaljkaCellType.DescRightDown)
                {
                    var cells = GetSlotCells(rows, r, c, KrizaljkaDirection.Right);
                    slots.Add(new KrizaljkaSlot(
                        GetNewSlotId(),
                        KrizaljkaDirection.Right,
                        r,
                        c,
                        cells.Count,
                        cells));
                }

                if (currentCell is KrizaljkaCellType.DescDown or KrizaljkaCellType.DescRightDown)
                {
                    var cells = GetSlotCells(rows, r, c, KrizaljkaDirection.Down);
                    slots.Add(new KrizaljkaSlot(
                        GetNewSlotId(),
                        KrizaljkaDirection.Down,
                        r,
                        c,
                        cells.Count,
                        cells));
                }
            }
        }


        return new KrizaljkaTemplateAnalysis(template.Id, template, slots, []);
    }

    private static IReadOnlyList<KrizaljkaCell> GetSlotCells(
        int[][] rows, 
        int slotRow, 
        int slotColumn, 
        KrizaljkaDirection direction)
    {
        List<KrizaljkaCell> cells = [];

        if (direction == KrizaljkaDirection.Right)
        {
            for (var c = ++slotColumn; c < rows[slotRow].Length; c++)
            {
                var currentCell = (KrizaljkaCellType)rows[slotRow][c];

                if (InputCells.Contains(currentCell))
                {
                    cells.Add(new KrizaljkaCell(slotRow, c, currentCell));
                }
                else
                {
                    break;
                }
            }
        }
        else if (direction == KrizaljkaDirection.Down)
        {
            for (var r = ++slotRow; r < rows.Length; r++)
            {
                var currentCell = (KrizaljkaCellType)rows[r][slotColumn];

                if (InputCells.Contains(currentCell))
                {
                    cells.Add(new KrizaljkaCell(slotRow, slotColumn, currentCell));
                }
                else
                {
                    break;
                }
            }
        }


        return cells.AsReadOnly();
    }

    private int GetNewSlotId()
    {
        return Interlocked.Increment(ref _lastSlotId);
    }
}

using Krizaljka.Domain.Template;
using System.Collections.Immutable;

namespace Krizaljka.Domain.TemplateAnalysis;

public class KrizaljkaAnalyzer
{
    private static readonly HashSet<KrizaljkaCellType> InputCells =
    [
        KrizaljkaCellType.Input, KrizaljkaCellType.InputColon, KrizaljkaCellType.InputDash
    ];

    private int _lastSlotId;

    public KrizaljkaTemplateAnalysis GeTemplateAnalysis(KrizaljkaTemplateBasic templateBasic)
    {
        if (templateBasic.Matrix.Length == 0)
        {
            return new KrizaljkaTemplateAnalysis(
                templateBasic,
                [],
                [],
                ImmutableDictionary<int, IReadOnlyList<KrizaljkaIntersection>>.Empty,
                ImmutableDictionary<int, IReadOnlyList<int>>.Empty,
                []);
        }

        var slots = GetSlots(templateBasic);
        var (intersections, cellSlots) = GetIntersections(slots);

        var neighborSlotsIdsBySlotId = GetNeighborSlotIdBySlotId(intersections);
        var intersectionsBySlotId = GetIntersectionsBySlotId(intersections);

        return new KrizaljkaTemplateAnalysis(templateBasic, slots, intersections, intersectionsBySlotId, neighborSlotsIdsBySlotId, cellSlots);
    }

    private static (IReadOnlyList<KrizaljkaIntersection>, Dictionary<(int, int), List<SlotUsage>>) GetIntersections(IReadOnlyList<KrizaljkaSlot> slots)
    {
        List<KrizaljkaIntersection> intersections = [];

        Dictionary<(int, int), List<SlotUsage>> usages = [];

        foreach (var slot in slots)
        {
            for (var i = 0; i < slot.Cells.Count; i++)
            {
                var currentCell = slot.Cells[i];
                var key = (currentCell.Row, currentCell.Col);
                var slotUsage = new SlotUsage(slot.Id, i);
                if (usages.TryGetValue(key, out var cellUsages))
                {
                    cellUsages.Add(slotUsage);
                }
                else
                {
                    usages.Add(key, [slotUsage]);
                }
            }
        }

        foreach (var (cell, cellSlots) in usages)
        {
            if (cellSlots.Count > 2)
            {
                throw new Exception(
                    $"Invalid templateBasic, it has more than 2 intersections on cell ({cell.Item1},{cell.Item2})");
            }

            // Valid intersection
            if (cellSlots.Count == 2)
            {
                intersections.Add(
                    new KrizaljkaIntersection(
                        cellSlots[0].SlotId,
                        cellSlots[1].SlotId,
                        cell.Item1,
                        cell.Item2,
                        cellSlots[0].CharIndex,
                        cellSlots[1].CharIndex));
            }
        }

        return (intersections.AsReadOnly(), usages);
    }

    private IReadOnlyList<KrizaljkaSlot> GetSlots(KrizaljkaTemplateBasic templateBasic)
    {
        List<KrizaljkaSlot> slots = [];

        var rows = templateBasic.Matrix;
        for (var r = 0; r < rows.Length; r++)
        {
            for (var c = 0; c < rows[r].Length; c++)
            {
                var currentCell = (KrizaljkaCellType)rows[r][c];

                if (currentCell is KrizaljkaCellType.DescRight or
                    KrizaljkaCellType.DescRightDown)
                {
                    var cells = GetSlotCells(rows, r, c, KrizaljkaDirection.Right);
                    if (cells.Count == 0)
                    {
                        continue;
                    }

                    slots.Add(new KrizaljkaSlot(
                        GetNewSlotId(),
                        KrizaljkaDirection.Right,
                        r,
                        c,
                        cells.Count,
                        cells,
                        cells
                            .Select(x => (x.Row, x.Col))
                            .ToList()
                            .AsReadOnly()));
                }

                if (currentCell is KrizaljkaCellType.DescDown or KrizaljkaCellType.DescRightDown)
                {
                    var cells = GetSlotCells(rows, r, c, KrizaljkaDirection.Down);
                    if (cells.Count == 0)
                    {
                        continue;
                    }

                    slots.Add(new KrizaljkaSlot(
                        GetNewSlotId(),
                        KrizaljkaDirection.Down,
                        r,
                        c,
                        cells.Count,
                        cells,
                        cells
                            .Select(x => (x.Row, x.Col))
                            .ToList()
                            .AsReadOnly()));
                }
            }
        }

        return slots.AsReadOnly();
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
                    cells.Add(new KrizaljkaCell(r, slotColumn, currentCell));
                }
                else
                {
                    break;
                }
            }
        }


        return cells.AsReadOnly();
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<int>> GetNeighborSlotIdBySlotId(
        IReadOnlyList<KrizaljkaIntersection> intersections)
    {
        Dictionary<int, HashSet<int>> map = [];

        foreach (var intersection in intersections)
        {
            if (!map.TryGetValue(intersection.FirstSlotId, out var first))
            {
                first = [];
                map.Add(intersection.FirstSlotId, first);
            }

            first.Add(intersection.SecondSlotId);

            if (!map.TryGetValue(intersection.SecondSlotId, out var second))
            {
                second = [];
                map.Add(intersection.SecondSlotId, second);
            }

            second.Add(intersection.FirstSlotId);
        }

        return map.ToDictionary(x => x.Key, x => (IReadOnlyList<int>)x.Value.ToList().AsReadOnly());
    }

    private static IReadOnlyDictionary<int, IReadOnlyList<KrizaljkaIntersection>> GetIntersectionsBySlotId(
        IReadOnlyList<KrizaljkaIntersection> intersections)
    {
        var map = new Dictionary<int, List<KrizaljkaIntersection>>();

        foreach (var intersection in intersections)
        {
            if (!map.TryGetValue(intersection.FirstSlotId, out var firstList))
            {
                firstList = [];
                map.Add(intersection.FirstSlotId, firstList);
            }

            firstList.Add(intersection);

            if (!map.TryGetValue(intersection.SecondSlotId, out var secondList))
            {
                secondList = [];
                map.Add(intersection.SecondSlotId, secondList);
            }

            secondList.Add(intersection);
        }

        return map.ToDictionary(
            x => x.Key,
            x => (IReadOnlyList<KrizaljkaIntersection>)x.Value.AsReadOnly());
    }

    private int GetNewSlotId()
    {
        return Interlocked.Increment(ref _lastSlotId);
    }
}

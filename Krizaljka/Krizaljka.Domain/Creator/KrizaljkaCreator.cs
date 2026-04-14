using System.Collections;
using Krizaljka.Domain.Caches;
using Krizaljka.Domain.Extensions;
using Krizaljka.Domain.TemplateAnalysis;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Creator;

public sealed class KrizaljkaCreator(TheKrizaljka theKrizaljka)
{
   //private CreatorCache _cache = new();
    private int _wordsPlacedDuringIterations;
    private IReadOnlyList<Term> _normalizedTerms = [];
    private Dictionary<int, SlotDomain> _currentDomainsBySlotId = [];
    private Dictionary<int, IReadOnlyList<TermGroup>> _termGroupsByLength = [];
    private Dictionary<long, (int Length, int GroupIndex)> _termGroupLocationById = [];
    private IReadOnlyDictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<string, BitArray>>> _termGroupBitsetsByLengthPositionLetter
        = new Dictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<string, BitArray>>>();



    public KrizaljkaCreateResult TrySolve(IReadOnlyList<Term> terms)
    {
        _wordsPlacedDuringIterations = 0;
        //_cache = new CreatorCache();

        _normalizedTerms = GetNormalizedTerms(terms);
        EnsureTermsCaches(_normalizedTerms);

        FinalizeFullyFilledUnassignedSlots();

        if (!TryInitializeDomains())
        {
            return new KrizaljkaCreateResult(false, theKrizaljka.State, _wordsPlacedDuringIterations);
        }

        var solved = Solve(theKrizaljka.Slots);
        return new KrizaljkaCreateResult(solved, theKrizaljka.State, _wordsPlacedDuringIterations);
    }

    public bool TryPlaceAssignedTermManually(
        IReadOnlyList<Term> terms,
        int slotId,
        long termId,
        out string? error)
    {
        if (_normalizedTerms.Count == 0)
        {
            _normalizedTerms = GetNormalizedTerms(terms);
            EnsureTermsCaches(_normalizedTerms);
        }

        return TryPlaceAssignedTerm(
            _normalizedTerms,
            slotId,
            termId,
            out error);
    }

    private static IReadOnlyList<Term> GetNormalizedTerms(IReadOnlyList<Term> terms) =>
        terms
            .Select(t => t with
            {
                Letters = t.Letters.Select(x => x.NormalizeLetters()).ToArray()
            })
            .ToList()
            .AsReadOnly();

   private void EnsureTermsCaches(IReadOnlyList<Term> normalizedTerms)
{
    if (GlobalCaches.TermsByLength.Count == 0)
    {
        GlobalCaches.TermsByLength = normalizedTerms
            .GroupBy(x => x.Length)
            .ToDictionary(
                x => x.Key,
                x => (IReadOnlyList<Term>)x
                    .OrderBy(t => t.DenseValue, StringComparer.Ordinal)
                    .ThenBy(t => t.Id)
                    .ToList());
    }

    if (_termGroupsByLength.Count == 0 ||
        _termGroupLocationById.Count == 0 ||
        _termGroupBitsetsByLengthPositionLetter.Count == 0)
    {
        var groupsByLength = new Dictionary<int, IReadOnlyList<TermGroup>>();
        var groupLocationById = new Dictionary<long, (int Length, int GroupIndex)>();
        var bitsetsByLength = new Dictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<string, BitArray>>>();

        foreach (var lengthEntry in GlobalCaches.TermsByLength)
        {
            var length = lengthEntry.Key;
            var allTerms = lengthEntry.Value;

            var groups = allTerms
                .GroupBy(term => CreateLettersKey(term.Letters))
                .Select(group =>
                {
                    var orderedTerms = group
                        .OrderBy(t => t.DenseValue, StringComparer.Ordinal)
                        .ThenBy(t => t.Id)
                        .ToList();

                    var representative = orderedTerms[0];
                    var termIds = orderedTerms
                        .Select(t => t.Id)
                        .ToList()
                        .AsReadOnly();

                    return new TermGroup(representative, termIds);
                })
                .OrderBy(g => g.Representative.DenseValue, StringComparer.Ordinal)
                .ThenBy(g => g.Representative.Id)
                .ToList();

            groupsByLength[length] = groups;

            for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
            {
                foreach (var termId in groups[groupIndex].TermIds)
                {
                    groupLocationById[termId] = (length, groupIndex);
                }
            }

            var positionMap = new Dictionary<int, IReadOnlyDictionary<string, BitArray>>();

            for (var position = 0; position < length; position++)
            {
                var letterMap = new Dictionary<string, BitArray>(StringComparer.Ordinal);

                for (var groupIndex = 0; groupIndex < groups.Count; groupIndex++)
                {
                    var letter = groups[groupIndex].Representative.Letters[position];

                    if (!letterMap.TryGetValue(letter, out var bitset))
                    {
                        bitset = new BitArray(groups.Count);
                        letterMap[letter] = bitset;
                    }

                    bitset[groupIndex] = true;
                }

                positionMap[position] = letterMap;
            }

            bitsetsByLength[length] = positionMap;
        }

        _termGroupsByLength = groupsByLength;
        _termGroupLocationById = groupLocationById;
        _termGroupBitsetsByLengthPositionLetter = bitsetsByLength;
    }
}

private static string CreateLettersKey(IReadOnlyList<string> letters) => string.Join("|", letters);

private bool TryPlaceAssignedTerm(
        IReadOnlyList<Term> terms,
        int slotId,
        long termId,
        out string? error)
    {
        error = null;

        if (theKrizaljka.State.IsAssigned(slotId))
        {
            error = "SlotAssigned";
            return false;
        }

        var slot = theKrizaljka.Slots.FirstOrDefault(x => x.Id == slotId);
        if (slot is null)
        {
            error = "SlotNotFound";
            return false;
        }

        var term = terms.FirstOrDefault(x => x.Id == termId);
        if (term is null)
        {
            error = "TermNotFound";
            return false;
        }

        if (!Fits(slot, term))
        {
            error = "TermDoesNotFit";
            return false;
        }

        Place(slot, term);
        return true;
    }

    private bool Solve(IReadOnlyList<KrizaljkaSlot> slots)
    {
        if (!TryGetBestNextSlot(slots, out var nextSlot))
        {
            return false;
        }

        if (nextSlot is null)
        {
            return true;
        }

        foreach (var term in GetOrderedTerms(nextSlot))
        {
            if (!Fits(nextSlot, term))
            {
                continue;
            }

            var domainSnapshot = CloneDomains();
            var initialPlacement = Place(nextSlot, term);

            if (!TryPropagateSingletonDomainsAfterPlacement(initialPlacement, out var fullPlacement))
            {
                Undo(fullPlacement);
                RestoreDomains(domainSnapshot);
                continue;
            }

            if (Solve(slots))
            {
                return true;
            }

            Undo(fullPlacement);
            RestoreDomains(domainSnapshot);
        }

        return false;
    }

    private bool TryPropagateSingletonDomainsAfterPlacement(
        PlacementResult initialPlacement,
        out PlacementResult fullPlacement)
    {
        List<(int SlotId, long TermId)> assignedSlots = initialPlacement.AssignedSlots.ToList();
        List<(int Row, int Col)> newCells = initialPlacement.NewCells.ToList();

        if (!UpdateDomainsAfterPlacement(initialPlacement))
        {
            fullPlacement = new PlacementResult(assignedSlots.AsReadOnly(), newCells.AsReadOnly());
            return false;
        }

        while (true)
        {
            var singletonSlot = FindSingletonUnassignedSlot();
            if (singletonSlot is null)
            {
                fullPlacement = new PlacementResult(assignedSlots.AsReadOnly(), newCells.AsReadOnly());
                return true;
            }

            if (!TryGetSingleCandidateTermFromDomain(singletonSlot, out var onlyTerm))
            {
                fullPlacement = new PlacementResult(assignedSlots.AsReadOnly(), newCells.AsReadOnly());
                return false;
            }

            if (!Fits(singletonSlot, onlyTerm))
            {
                fullPlacement = new PlacementResult(assignedSlots.AsReadOnly(), newCells.AsReadOnly());
                return false;
            }

            var singletonPlacement = Place(singletonSlot, onlyTerm);

            assignedSlots.AddRange(singletonPlacement.AssignedSlots);
            newCells.AddRange(singletonPlacement.NewCells);

            if (!UpdateDomainsAfterPlacement(singletonPlacement))
            {
                fullPlacement = new PlacementResult(assignedSlots.AsReadOnly(), newCells.AsReadOnly());
                return false;
            }
        }
    }

    private bool TryGetSingleCandidateTermFromDomain(KrizaljkaSlot slot, out Term term)
    {
        term = null!;

        if (!_currentDomainsBySlotId.TryGetValue(slot.Id, out var domain) || domain.Count != 1)
        {
            return false;
        }

        foreach (var groupIndex in EnumerateSetBits(domain.Candidates))
        {
            return TryCreateAvailableTermFromGroup(slot.Length, groupIndex, out term);
        }

        return false;
    }

    private KrizaljkaSlot? FindSingletonUnassignedSlot()
    {
        foreach (var kvp in _currentDomainsBySlotId)
        {
            if (kvp.Value.Count != 1)
            {
                continue;
            }

            if (!theKrizaljka.SlotsById.TryGetValue(kvp.Key, out var slot))
            {
                continue;
            }

            if (theKrizaljka.State.IsAssigned(slot.Id))
            {
                continue;
            }

            return slot;
        }

        return null;
    }

    private bool TryGetBestNextSlot(
        IReadOnlyList<KrizaljkaSlot> slots,
        out KrizaljkaSlot? bestSlot)
    {
        bestSlot = null;
        var bestCount = int.MaxValue;
        var bestUnassignedNeighborCount = -1;

        foreach (var slot in slots)
        {
            if (theKrizaljka.State.IsAssigned(slot.Id))
            {
                continue;
            }

            if (!_currentDomainsBySlotId.TryGetValue(slot.Id, out var domain) || domain.Count == 0)
            {
                return false;
            }

            var fittingCount = domain.Count;
            var unassignedNeighborCount = GetUnassignedNeighborCount(slot);

            if (fittingCount < bestCount ||
                (fittingCount == bestCount && unassignedNeighborCount > bestUnassignedNeighborCount))
            {
                bestCount = fittingCount;
                bestUnassignedNeighborCount = unassignedNeighborCount;
                bestSlot = slot;

                if (bestCount == 1)
                {
                    return true;
                }
            }
        }

        return true;
    }

    private bool TryInitializeDomains()
    {
        _currentDomainsBySlotId = [];
        foreach (var slot in theKrizaljka.Slots)
        {
            if (theKrizaljka.State.IsAssigned(slot.Id))
            {
                continue;
            }

            var domain = BuildDomain(slot);

            if (domain.Count == 0)
            {
                return false;
            }

            _currentDomainsBySlotId[slot.Id] = domain;
        }

        return true;
    }

    private SlotDomain BuildDomain(KrizaljkaSlot slot)
    {
        var candidates = BuildMatchingMaskForSlot(slot);

        if (candidates.Length == 0)
        {
            return new SlotDomain(candidates, 0);
        }

        for (var groupIndex = 0; groupIndex < candidates.Length; groupIndex++)
        {
            if (!candidates[groupIndex])
            {
                continue;
            }

            if (!HasAvailableUnusedTermId(slot.Length, groupIndex))
            {
                candidates[groupIndex] = false;
            }
        }

        var count = CountBits(candidates);
        return new SlotDomain(candidates, count);
    }

    private bool HasAvailableUnusedTermId(int length, int groupIndex)
    {
        if (!_termGroupsByLength.TryGetValue(length, out var groups))
        {
            return false;
        }

        if (groupIndex < 0 || groupIndex >= groups.Count)
        {
            return false;
        }

        foreach (var termId in groups[groupIndex].TermIds)
        {
            if (!theKrizaljka.State.UsedTermsIds.Contains(termId))
            {
                return true;
            }
        }

        return false;
    }

    private Dictionary<int, SlotDomain> CloneDomains() =>
        _currentDomainsBySlotId.ToDictionary(x => x.Key, x => x.Value.Clone());

    private bool UpdateDomainsAfterPlacement(PlacementResult placement)
{
    HashSet<int> affectedSlotIds = [];

    foreach (var (assignedSlotId, termId) in placement.AssignedSlots)
    {
        _currentDomainsBySlotId.Remove(assignedSlotId);

        if (_termGroupLocationById.TryGetValue(termId, out var groupLocation) &&
            !HasAvailableUnusedTermId(groupLocation.Length, groupLocation.GroupIndex))
        {
            foreach (var domainEntry in _currentDomainsBySlotId)
            {
                if (!theKrizaljka.SlotsById.TryGetValue(domainEntry.Key, out var currentSlot))
                {
                    return false;
                }

                if (currentSlot.Length != groupLocation.Length)
                {
                    continue;
                }

                if (domainEntry.Value.Candidates[groupLocation.GroupIndex])
                {
                    domainEntry.Value.Candidates[groupLocation.GroupIndex] = false;
                    domainEntry.Value.Count--;
                }
            }
        }

        if (!theKrizaljka.IntersectionsBySlotId.TryGetValue(assignedSlotId, out var intersections))
        {
            continue;
        }

        foreach (var intersection in intersections)
        {
            var neighborSlotId = intersection.FirstSlotId == assignedSlotId
                ? intersection.SecondSlotId
                : intersection.FirstSlotId;

            if (!theKrizaljka.State.IsAssigned(neighborSlotId))
            {
                affectedSlotIds.Add(neighborSlotId);
            }
        }
    }

    foreach (var slotId in affectedSlotIds)
    {
        if (!theKrizaljka.SlotsById.TryGetValue(slotId, out var slot))
        {
            return false;
        }

        var domain = BuildDomain(slot);
        _currentDomainsBySlotId[slotId] = domain;
    }

    foreach (var domainEntry in _currentDomainsBySlotId)
    {
        if (domainEntry.Value.Count == 0)
        {
            return false;
        }
    }

    return true;
}

    private void RestoreDomains(Dictionary<int, SlotDomain> snapshot) => _currentDomainsBySlotId = snapshot;

    private int GetUnassignedNeighborCount(KrizaljkaSlot slot)
    {
        if (!theKrizaljka.NeighborSlotsIdsBySlotId.TryGetValue(slot.Id, out var neighborSlotIds))
        {
            return 0;
        }

        var count = 0;

        foreach (var neighborSlotId in neighborSlotIds)
        {
            if (!theKrizaljka.State.IsAssigned(neighborSlotId))
            {
                count++;
            }
        }

        return count;
    }

    private bool Fits(KrizaljkaSlot slot, Term term)
    {
        if (theKrizaljka.State.IsAssigned(slot.Id))
        {
            return false;
        }

        if (theKrizaljka.State.UsedTermsIds.Contains(term.Id))
        {
            return false;
        }

        if (term.Length != slot.Length)
        {
            return false;
        }

        for (var i = 0; i < slot.Cells.Count; i++)
        {
            var termLetter = term.Letters[i];

            if (theKrizaljka.State.LettersByCell.TryGetValue(slot.CellKeys[i], out var existingLetter) &&
                !string.Equals(existingLetter, termLetter, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private PlacementResult Place(KrizaljkaSlot slot, Term term)
    {
        List<(int Row, int Col)> newCells = [];
        List<(int SlotId, long TermId)> assignedSlots = [];

        AssignSlot(slot, term, newCells, assignedSlots);

        Queue<int> queue = new();
        queue.Enqueue(slot.Id);

        HashSet<int> queuedOrProcessed = [slot.Id];

        while (queue.Count > 0)
        {
            var currentSlotId = queue.Dequeue();

            if (!theKrizaljka.IntersectionsBySlotId.TryGetValue(currentSlotId, out var intersections))
            {
                continue;
            }

            foreach (var intersection in intersections)
            {
                var neighborSlotId = intersection.FirstSlotId == currentSlotId
                    ? intersection.SecondSlotId
                    : intersection.FirstSlotId;

                if (!queuedOrProcessed.Add(neighborSlotId))
                {
                    continue;
                }

                if (theKrizaljka.State.IsAssigned(neighborSlotId))
                {
                    continue;
                }

                if (!theKrizaljka.SlotsById.TryGetValue(neighborSlotId, out var neighborSlot))
                {
                    continue;
                }

                if (!IsFullyFilled(neighborSlot))
                {
                    continue;
                }

                if (!TryGetFirstUnusedMatchingTerm(neighborSlot, out var matchingTerm))
                {
                    continue;
                }

                AssignSlot(neighborSlot, matchingTerm, newCells, assignedSlots);
                queue.Enqueue(neighborSlotId);
            }
        }

        return new PlacementResult(assignedSlots.AsReadOnly(), newCells.AsReadOnly());
    }

    private bool IsFullyFilled(KrizaljkaSlot slot)
    {
        for (var i = 0; i < slot.Cells.Count; i++)
        {
            if (!theKrizaljka.State.LettersByCell.ContainsKey(slot.CellKeys[i]))
            {
                return false;
            }
        }

        return true;
    }

    private void AssignSlot(
        KrizaljkaSlot slot,
        Term term,
        List<(int Row, int Col)> newCells,
        List<(int SlotId, long TermId)> assignedSlots)
    {
        _wordsPlacedDuringIterations++;
        theKrizaljka.State.AssignedTermsBySlotId.Add(
            slot.Id,
            new AssignedTerm(slot.Id, term.Id, term.Letters));

        theKrizaljka.State.UsedTermsIds.Add(term.Id);
        assignedSlots.Add((slot.Id, term.Id));

        for (var i = 0; i < slot.Cells.Count; i++)
        {
            var slotCell = slot.Cells[i];
            var key = (slotCell.Row, slotCell.Col);

            if (!theKrizaljka.State.LettersByCell.ContainsKey(key))
            {
                theKrizaljka.State.LettersByCell.Add(key, term.Letters[i]);
                newCells.Add(key);
            }
        }
    }

    private void Undo(PlacementResult placement)
    {
        foreach (var (slotId, termId) in placement.AssignedSlots)
        {
            theKrizaljka.State.AssignedTermsBySlotId.Remove(slotId);
            theKrizaljka.State.UsedTermsIds.Remove(termId);
        }

        foreach (var cell in placement.NewCells)
        {
            theKrizaljka.State.LettersByCell.Remove(cell);
        }
    }

    //private IReadOnlyList<Term> GetIndexedMatchingTerms(KrizaljkaSlot slot) => GetIndexedMatchingTermsCore(slot);

    //private string GetSlotPattern(KrizaljkaSlot slot)
    //{
    //    var state = theKrizaljka.State;
    //    var parts = new string[slot.Cells.Count];


    //    for (var i = 0; i < slot.Cells.Count; i++)
    //    {

    //        if (state.LettersByCell.TryGetValue(slot.CellKeys[i], out var letter) &&
    //            !string.IsNullOrWhiteSpace(letter))
    //        {
    //            parts[i] = letter.NormalizeLetters();
    //        }
    //        else
    //        {
    //            parts[i] = "_";
    //        }
    //    }

    //    return string.Join('|', parts);
    //}

    //private IReadOnlyList<Term> GetIndexedMatchingTermsCore(KrizaljkaSlot slot)
    //{
    //    if (!_termGroupsByLength.TryGetValue(slot.Length, out _))
    //    {
    //        return [];
    //    }

    //    var candidates = BuildMatchingMaskForSlot(slot);
    //    List<Term> result = [];

    //    foreach (var groupIndex in EnumerateSetBits(candidates))
    //    {
    //        if (TryCreateAvailableTermFromGroup(slot.Length, groupIndex, out var term))
    //        {
    //            result.Add(term);
    //        }
    //    }

    //    return result;
    //}
    
    private List<Term> GetOrderedTerms(KrizaljkaSlot slot)
    {
        if (!_currentDomainsBySlotId.TryGetValue(slot.Id, out var domain))
        {
            return [];
        }

        List<Term> result = [];

        foreach (var groupIndex in EnumerateSetBits(domain.Candidates))
        {
            if (TryCreateAvailableTermFromGroup(slot.Length, groupIndex, out var candidate))
            {
                result.Add(candidate);
            }
        }

        return result;
    }

    //private List<Term> GetOrderedTerms(KrizaljkaSlot slot)
    //{
    //    List<(Term Term, int Score)> scored = [];

    //    foreach (var term in GetIndexedMatchingTerms(slot))
    //    {
    //        if (theKrizaljka.State.UsedTermsIds.Contains(term.Id))
    //        {
    //            continue;
    //        }

    //        var score = ScoreCandidateTerm(slot, term);

    //        if (score < 0)
    //        {
    //            continue;
    //        }

    //        scored.Add((term, score));
    //    }

    //    return scored
    //        .OrderByDescending(x => x.Score)
    //        .ThenBy(x => x.Term.DenseValue, StringComparer.Ordinal)
    //        .Select(x => x.Term)
    //        .ToList();
    //}

    //private int ScoreCandidateTerm(KrizaljkaSlot slot, Term term)
    //{
    //    if (!theKrizaljka.IntersectionsBySlotId.TryGetValue(slot.Id, out var intersections))
    //    {
    //        return 0;
    //    }

    //    var totalScore = 0;

    //    foreach (var intersection in intersections)
    //    {
    //        var neighborSlotId = intersection.FirstSlotId == slot.Id
    //            ? intersection.SecondSlotId
    //            : intersection.FirstSlotId;

    //        if (theKrizaljka.State.IsAssigned(neighborSlotId))
    //        {
    //            continue;
    //        }

    //        if (!theKrizaljka.SlotsById.TryGetValue(neighborSlotId, out var neighborSlot))
    //        {
    //            return -1;
    //        }

    //        var count = CountNeighborCandidatesAfterPlacingTerm(slot, term, neighborSlot);

    //        if (count == 0)
    //        {
    //            return -1;
    //        }

    //        totalScore += Math.Min(count, 1000);
    //    }

    //    return totalScore;
    //}

    //private int CountNeighborCandidatesAfterPlacingTerm(
    //    KrizaljkaSlot sourceSlot,
    //    Term sourceTerm,
    //    KrizaljkaSlot neighborSlot)
    //{
    //    if (!GlobalCaches.TermsByLength.TryGetValue(neighborSlot.Length, out var allTerms))
    //    {
    //        return 0;
    //    }

    //    if (!_cache.TermsByLengthPositionLetter.TryGetValue(neighborSlot.Length, out var byPosition))
    //    {
    //        var countWithoutIndex = 0;

    //        foreach (var term in allTerms)
    //        {
    //            if (theKrizaljka.State.UsedTermsIds.Contains(term.Id) || term.Id == sourceTerm.Id)
    //            {
    //                continue;
    //            }

    //            countWithoutIndex++;
    //        }

    //        return countWithoutIndex;
    //    }

    //    List<IReadOnlyList<Term>> constrainedLists = [];

    //    for (var i = 0; i < neighborSlot.Cells.Count; i++)
    //    {
    //        var effectiveLetter = GetEffectiveLetterForNeighborCell(sourceSlot, sourceTerm, neighborSlot.CellKeys[i]);

    //        if (effectiveLetter is null)
    //        {
    //            continue;
    //        }

    //        if (!byPosition.TryGetValue(i, out var byLetter))
    //        {
    //            return 0;
    //        }

    //        if (!byLetter.TryGetValue(effectiveLetter, out var matchingTerms))
    //        {
    //            return 0;
    //        }

    //        constrainedLists.Add(matchingTerms);
    //    }

    //    IReadOnlyList<Term> baseList;

    //    if (constrainedLists.Count == 0)
    //    {
    //        baseList = allTerms;
    //    }
    //    else
    //    {
    //        baseList = constrainedLists
    //            .OrderBy(x => x.Count)
    //            .First();
    //    }

    //    var count = 0;

    //    foreach (var term in baseList)
    //    {
    //        if (theKrizaljka.State.UsedTermsIds.Contains(term.Id) || term.Id == sourceTerm.Id)
    //        {
    //            continue;
    //        }

    //        var matches = true;

    //        for (var i = 0; i < neighborSlot.Cells.Count; i++)
    //        {
    //            var effectiveLetter =
    //                GetEffectiveLetterForNeighborCell(sourceSlot, sourceTerm, neighborSlot.CellKeys[i]);

    //            if (effectiveLetter is not null &&
    //                !string.Equals(term.Letters[i], effectiveLetter, StringComparison.Ordinal))
    //            {
    //                matches = false;
    //                break;
    //            }
    //        }

    //        if (matches)
    //        {
    //            count++;
    //        }
    //    }

    //    return count;
    //}

    //private string? GetEffectiveLetterForNeighborCell(
    //    KrizaljkaSlot sourceSlot,
    //    Term sourceTerm,
    //    (int Row, int Col) neighborCellKey)
    //{
    //    if (theKrizaljka.State.LettersByCell.TryGetValue(neighborCellKey, out var existingLetter))
    //    {
    //        return existingLetter.NormalizeLetters();
    //    }

    //    for (var i = 0; i < sourceSlot.CellKeys.Count; i++)
    //    {
    //        if (sourceSlot.CellKeys[i] == neighborCellKey)
    //        {
    //            return sourceTerm.Letters[i];
    //        }
    //    }

    //    return null;
    //}

    private void FinalizeFullyFilledUnassignedSlots()
    {
        var changed = true;
        while (changed)
        {
            changed = false;

            foreach (var slot in theKrizaljka.Slots)
            {
                if (theKrizaljka.State.IsAssigned(slot.Id))
                {
                    continue;
                }

                if (!IsFullyFilled(slot))
                {
                    continue;
                }

                if (!TryGetFirstUnusedMatchingTerm(slot, out var matchingTerm))
                {
                    continue;
                }

                Place(slot, matchingTerm);
                changed = true;
            }
        }
    }

    private bool TryGetFirstUnusedMatchingTerm(KrizaljkaSlot slot, out Term term)
    {
        term = null!;

        var candidates = BuildMatchingMaskForSlot(slot);

        foreach (var groupIndex in EnumerateSetBits(candidates))
        {
            if (TryCreateAvailableTermFromGroup(slot.Length, groupIndex, out term))
            {
                return true;
            }
        }

        return false;
    }

    private BitArray BuildMatchingMaskForSlot(KrizaljkaSlot slot)
    {
        if (!_termGroupsByLength.TryGetValue(slot.Length, out var groups))
        {
            return new BitArray(0);
        }

        var result = new BitArray(groups.Count, true);

        if (!_termGroupBitsetsByLengthPositionLetter.TryGetValue(slot.Length, out var byPosition))
        {
            return result;
        }

        for (var i = 0; i < slot.Cells.Count; i++)
        {
            if (!theKrizaljka.State.LettersByCell.TryGetValue(slot.CellKeys[i], out var existingLetter))
            {
                continue;
            }

            var normalizedExistingLetter = existingLetter.NormalizeLetters();

            if (!byPosition.TryGetValue(i, out var byLetter))
            {
                return new BitArray(groups.Count, false);
            }

            if (!byLetter.TryGetValue(normalizedExistingLetter, out var matchingBitset))
            {
                return new BitArray(groups.Count, false);
            }

            result.And(matchingBitset);
        }

        return result;
    }

    private bool TryCreateAvailableTermFromGroup(int length, int groupIndex, out Term term)
    {
        term = null!;

        if (!_termGroupsByLength.TryGetValue(length, out var groups))
        {
            return false;
        }

        if (groupIndex < 0 || groupIndex >= groups.Count)
        {
            return false;
        }

        var group = groups[groupIndex];

        foreach (var termId in group.TermIds)
        {
            if (!theKrizaljka.State.UsedTermsIds.Contains(termId))
            {
                term = group.Representative with { Id = termId };
                return true;
            }
        }

        return false;
    }

    private static IEnumerable<int> EnumerateSetBits(BitArray bits)
    {
        for (var i = 0; i < bits.Length; i++)
        {
            if (bits[i])
            {
                yield return i;
            }
        }
    }

    private static int CountBits(BitArray bits)
    {
        var count = 0;

        for (var i = 0; i < bits.Length; i++)
        {
            if (bits[i])
            {
                count++;
            }
        }

        return count;
    }
}

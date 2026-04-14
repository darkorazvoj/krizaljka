using Krizaljka.Domain.Caches;
using Krizaljka.Domain.Extensions;
using Krizaljka.Domain.TemplateAnalysis;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Creator;

public sealed class KrizaljkaCreator(TheKrizaljka theKrizaljka)
{
    private CreatorCache _cache = new();
    private int _wordsPlacedDuringIterations;
    private IReadOnlyList<Term> _normalizedTerms = [];
    private Dictionary<int, IReadOnlyList<Term>> _currentDomainsBySlotId = [];

    public KrizaljkaCreateResult TrySolve(IReadOnlyList<Term> terms)
    {
        _wordsPlacedDuringIterations = 0;
        _cache = new CreatorCache();

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
                    x => (IReadOnlyList<Term>)x.OrderBy(t => t.DenseValue, StringComparer.OrdinalIgnoreCase)
                        .ToList());
        }

        if (_cache.TermsByLengthPositionLetter.Count == 0)
        {
            var result =
                new Dictionary<int, IReadOnlyDictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Term>>>>();

            foreach (var lengthGroup in normalizedTerms.GroupBy(x => x.Length))
            {
                var positionMap = new Dictionary<int, IReadOnlyDictionary<string, IReadOnlyList<Term>>>();

                for (var i = 0; i < lengthGroup.Key; i++)
                {
                    var letterMap = lengthGroup
                        .GroupBy(term => term.Letters[i])
                        .ToDictionary(
                            x => x.Key,
                            x => (IReadOnlyList<Term>)x
                                .OrderBy(t => t.DenseValue, StringComparer.OrdinalIgnoreCase)
                                .ToList());

                    positionMap.Add(i, letterMap);
                }

                result.Add(lengthGroup.Key, positionMap);
            }

            _cache.TermsByLengthPositionLetter = result;
        }
    }

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

            //var placement = Place(nextSlot, term);

            //if (!UpdateDomainsAfterPlacement(placement))
            //{
            //    Undo(placement);
            //    RestoreDomains(domainSnapshot);
            //    continue;
            //}

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
        List<(int SliotId, long TermId)> assignedSlots = initialPlacement.AssignedSlots.ToList();
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

            if (!_currentDomainsBySlotId.TryGetValue(singletonSlot.Id, out var domain) || domain.Count != 1)
            {
                fullPlacement = new PlacementResult(assignedSlots.AsReadOnly(), newCells.AsReadOnly());
                return false;
            }

            var onlyTerm = domain[0];

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

            var domain = GetIndexedMatchingTerms(slot)
                .Where(x => !theKrizaljka.State.UsedTermsIds.Contains(x.Id))
                .ToList();

            if (domain.Count == 0)
            {
                return false;
            }

            _currentDomainsBySlotId[slot.Id] = domain;
        }

        return true;
    }

    private Dictionary<int, IReadOnlyList<Term>> CloneDomains() =>
        _currentDomainsBySlotId.ToDictionary(x => x.Key, x => x.Value);

    private bool UpdateDomainsAfterPlacement(PlacementResult placement)
    {
        var newlyUsedTermIds = placement.AssignedSlots
            .Select(x => x.TermId)
            .ToHashSet();

        HashSet<int> affectedSlotIds = [];

        foreach (var (assignedSlotIt, _) in placement.AssignedSlots)
        {
            _currentDomainsBySlotId.Remove(assignedSlotIt);
            if (!theKrizaljka.IntersectionsBySlotId.TryGetValue(assignedSlotIt, out var intersections))
            {
                continue;
            }

            foreach (var intersection in intersections)
            {
                var neighborSlotId = intersection.FirstSlotId == assignedSlotIt
                    ? intersection.SecondSlotId
                    : intersection.FirstSlotId;

                if (!theKrizaljka.State.IsAssigned(neighborSlotId))
                {
                    affectedSlotIds.Add(neighborSlotId);
                }
            }
        }

        foreach (var slotId in _currentDomainsBySlotId.Keys.ToList())
        {
            var filtered = _currentDomainsBySlotId[slotId]
                .Where(x => !newlyUsedTermIds.Contains(x.Id))
                .ToList();

            _currentDomainsBySlotId[slotId] = filtered;
        }

        foreach (var slotId in affectedSlotIds)
        {
            if (!theKrizaljka.SlotsById.TryGetValue(slotId, out var slot))
            {
                return false;
            }

            var domain = GetIndexedMatchingTerms(slot)
                .Where(x => !theKrizaljka.State.UsedTermsIds.Contains(x.Id))
                .ToList();

            _currentDomainsBySlotId[slotId] = domain;
        }

        foreach (var slot in theKrizaljka.Slots)
        {
            if (theKrizaljka.State.IsAssigned(slot.Id))
            {
                continue;
            }

            if (!_currentDomainsBySlotId.TryGetValue(slot.Id, out var domain) || domain.Count == 0)
            {
                return false;
            }
        }

        return true;
    }

    private void RestoreDomains(Dictionary<int, IReadOnlyList<Term>> snapshot) => _currentDomainsBySlotId = snapshot;

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

                var matchingTerm = GetIndexedMatchingTerms(neighborSlot)
                    .FirstOrDefault(x => !theKrizaljka.State.UsedTermsIds.Contains(x.Id));

                if (matchingTerm is null)
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

    private IReadOnlyList<Term> GetIndexedMatchingTerms(KrizaljkaSlot slot)
    {
        var pattern = GetSlotPattern(slot);
        var cacheKey = (slot.Id, pattern);

        if (_cache.MatchingTermsCache.TryGetValue(cacheKey, out var cached))
        {
            return cached;
        }

        var result = GetIndexedMatchingTermsCore(slot);
        _cache.MatchingTermsCache[cacheKey] = result;

        return result;
    }

    private string GetSlotPattern(KrizaljkaSlot slot)
    {
        var state = theKrizaljka.State;
        var parts = new string[slot.Cells.Count];


        for (var i = 0; i < slot.Cells.Count; i++)
        {

            if (state.LettersByCell.TryGetValue(slot.CellKeys[i], out var letter) &&
                !string.IsNullOrWhiteSpace(letter))
            {
                parts[i] = letter.NormalizeLetters();
            }
            else
            {
                parts[i] = "_";
            }
        }

        return string.Join('|', parts);
    }

    private IReadOnlyList<Term> GetIndexedMatchingTermsCore(KrizaljkaSlot slot)
    {
        if (!GlobalCaches.TermsByLength.TryGetValue(slot.Length, out var allTerms))
        {
            return [];
        }

        if (!_cache.TermsByLengthPositionLetter.TryGetValue(slot.Length, out var byPosition))
        {
            return allTerms;
        }

        List<IReadOnlyList<Term>> constrainedLists = [];

        for (var i = 0; i < slot.Cells.Count; i++)
        {
            if (!theKrizaljka.State.LettersByCell.TryGetValue(slot.CellKeys[i], out var existingLetter))
            {
                continue;
            }

            if (!byPosition.TryGetValue(i, out var byLetter))
            {
                return [];
            }

            var normalizedExistingLetter = existingLetter.NormalizeLetters();

            if (!byLetter.TryGetValue(normalizedExistingLetter, out var matchingTerms))
            {
                return [];
            }

            constrainedLists.Add(matchingTerms);
        }

        if (constrainedLists.Count == 0)
        {
            return allTerms;
        }

        var smallest = constrainedLists
            .OrderBy(x => x.Count)
            .First();

        List<Term> result = [];

        foreach (var term in smallest)
        {
            var matches = true;

            for (var i = 0; i < slot.Cells.Count; i++)
            {
                if (theKrizaljka.State.LettersByCell.TryGetValue(slot.CellKeys[i], out var existingLetter) &&
                    !string.Equals(term.Letters[i], existingLetter.NormalizeLetters(), StringComparison.Ordinal))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                result.Add(term);
            }
        }

        return result;
    }
    
    private List<Term> GetOrderedTerms(KrizaljkaSlot slot)
    {
        List<(Term Term, int Score)> scored = [];

        foreach (var term in GetIndexedMatchingTerms(slot))
        {
            if (theKrizaljka.State.UsedTermsIds.Contains(term.Id))
            {
                continue;
            }

            var score = ScoreCandidateTerm(slot, term);

            if (score < 0)
            {
                continue;
            }

            scored.Add((term, score));
        }

        return scored
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Term.DenseValue, StringComparer.Ordinal)
            .Select(x => x.Term)
            .ToList();
    }

    private int ScoreCandidateTerm(KrizaljkaSlot slot, Term term)
    {
        if (!theKrizaljka.IntersectionsBySlotId.TryGetValue(slot.Id, out var intersections))
        {
            return 0;
        }

        var totalScore = 0;

        foreach (var intersection in intersections)
        {
            var neighborSlotId = intersection.FirstSlotId == slot.Id
                ? intersection.SecondSlotId
                : intersection.FirstSlotId;

            if (theKrizaljka.State.IsAssigned(neighborSlotId))
            {
                continue;
            }

            if (!theKrizaljka.SlotsById.TryGetValue(neighborSlotId, out var neighborSlot))
            {
                return -1;
            }

            var count = CountNeighborCandidatesAfterPlacingTerm(slot, term, neighborSlot);

            if (count == 0)
            {
                return -1;
            }

            totalScore += Math.Min(count, 1000);
        }

        return totalScore;
    }

    private int CountNeighborCandidatesAfterPlacingTerm(
        KrizaljkaSlot sourceSlot,
        Term sourceTerm,
        KrizaljkaSlot neighborSlot)
    {
        if (!GlobalCaches.TermsByLength.TryGetValue(neighborSlot.Length, out var allTerms))
        {
            return 0;
        }

        if (!_cache.TermsByLengthPositionLetter.TryGetValue(neighborSlot.Length, out var byPosition))
        {
            var countWithoutIndex = 0;

            foreach (var term in allTerms)
            {
                if (theKrizaljka.State.UsedTermsIds.Contains(term.Id) || term.Id == sourceTerm.Id)
                {
                    continue;
                }

                countWithoutIndex++;
            }

            return countWithoutIndex;
        }

        List<IReadOnlyList<Term>> constrainedLists = [];

        for (var i = 0; i < neighborSlot.Cells.Count; i++)
        {
            var effectiveLetter = GetEffectiveLetterForNeighborCell(sourceSlot, sourceTerm, neighborSlot.CellKeys[i]);

            if (effectiveLetter is null)
            {
                continue;
            }

            if (!byPosition.TryGetValue(i, out var byLetter))
            {
                return 0;
            }

            if (!byLetter.TryGetValue(effectiveLetter, out var matchingTerms))
            {
                return 0;
            }

            constrainedLists.Add(matchingTerms);
        }

        IReadOnlyList<Term> baseList;

        if (constrainedLists.Count == 0)
        {
            baseList = allTerms;
        }
        else
        {
            baseList = constrainedLists
                .OrderBy(x => x.Count)
                .First();
        }

        var count = 0;

        foreach (var term in baseList)
        {
            if (theKrizaljka.State.UsedTermsIds.Contains(term.Id) || term.Id == sourceTerm.Id)
            {
                continue;
            }

            var matches = true;

            for (var i = 0; i < neighborSlot.Cells.Count; i++)
            {
                var effectiveLetter =
                    GetEffectiveLetterForNeighborCell(sourceSlot, sourceTerm, neighborSlot.CellKeys[i]);

                if (effectiveLetter is not null &&
                    !string.Equals(term.Letters[i], effectiveLetter, StringComparison.Ordinal))
                {
                    matches = false;
                    break;
                }
            }

            if (matches)
            {
                count++;
            }
        }

        return count;
    }

    private string? GetEffectiveLetterForNeighborCell(
        KrizaljkaSlot sourceSlot,
        Term sourceTerm,
        (int Row, int Col) neighborCellKey)
    {
        if (theKrizaljka.State.LettersByCell.TryGetValue(neighborCellKey, out var existingLetter))
        {
            return existingLetter.NormalizeLetters();
        }

        for (var i = 0; i < sourceSlot.CellKeys.Count; i++)
        {
            if (sourceSlot.CellKeys[i] == neighborCellKey)
            {
                return sourceTerm.Letters[i];
            }
        }

        return null;
    }

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

                var matchingTerm = GetIndexedMatchingTerms(slot)
                    .FirstOrDefault(x => !theKrizaljka.State.UsedTermsIds.Contains(x.Id));

                if (matchingTerm is null)
                {
                    continue;
                }

                Place(slot, matchingTerm);
                changed = true;
            }
        }

    }
}

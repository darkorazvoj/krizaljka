using System.Diagnostics;
using Krizaljka.Domain.Caches;
using Krizaljka.Domain.Extensions;
using Krizaljka.Domain.TemplateAnalysis;
using Krizaljka.Domain.Terms;

namespace Krizaljka.Domain.Creator;

public sealed class KrizaljkaCreator(TheKrizaljka theKrizaljka)
{
    private CreatorCache _cache = new();
    private IReadOnlyList<Term> _normalizedTerms = [];
    private Dictionary<long, Term> _normalizedTermsById = [];
    private Dictionary<int, IReadOnlyDictionary<string, IReadOnlyList<long>>> _termIdsByLengthAndLettersKey = [];
    private Dictionary<int, IReadOnlyList<Term>> _currentDomainsBySlotId = [];
    private CancellationToken _stopToken;

    private readonly Stopwatch _stopwatch = new();
    private long _recursiveCalls;
    private long _candidateTries;
    private long _backtracks;
    private long _deadEnds;
    private long _fullyFilledAutoAssignments;
    private long _singletonAutoAssignments;
    private int _maxAssignedSlotsReached;
    private long? _maxSolveElapsedMilliseconds;
    private bool _timedOut;
    private KrizaljkaSolveState? _bestStateSnapshot;

    public KrizaljkaCreateResult TrySolve(
        IReadOnlyList<Term> terms, 
        int maxMinutes, 
        CancellationToken stopToken)
    {
        ResetStats();
        _cache = new CreatorCache();
        _stopToken = stopToken;

        _normalizedTerms = GetNormalizedTerms(terms);
        EnsureTermsCaches(_normalizedTerms);

        _maxSolveElapsedMilliseconds = maxMinutes > 0
            ? (long)(maxMinutes * 60_000d)
            : null;

        CaptureBestStateIfNeeded();

        _stopwatch.Start();
        try
        {
            if (!TryFinalizeForcedPreSolveAssignments())
            {
                return new KrizaljkaCreateResult(
                    false,
                    theKrizaljka.State,
                    GetBestStateSnapshot(),
                    BuildSolveStats(false));
            }


            if (HasTimedOut())
            {
                return new KrizaljkaCreateResult(
                    false,
                    theKrizaljka.State,
                    GetBestStateSnapshot(),
                    BuildSolveStats(false));
            }

            var solved = Solve(theKrizaljka.Slots);
            return new KrizaljkaCreateResult(
                solved && !_timedOut,
                theKrizaljka.State,
                GetBestStateSnapshot(),
                BuildSolveStats(solved && !_timedOut));
        }
        finally
        {
            _stopwatch.Stop();
        }
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

    private bool TryFinalizeForcedPreSolveAssignments()
    {
        while (true)
        {
            if (HasTimedOut())
            {
                return false;
            }

            if (!TryInitializeDomains())
            {
                return false;
            }

            if (!TryGetForcedPreSolveAssignment(
                    out var forcedSlot,
                    out var forcedTerm,
                    out var isSingletonAssignment))
            {
                return true;
            }

            if (!Fits(forcedSlot!, forcedTerm!))
            {
                return false;
            }

            if (isSingletonAssignment)
            {
                _singletonAutoAssignments++;
            }
            else
            {
                _fullyFilledAutoAssignments++;
            }

            Place(forcedSlot!, forcedTerm!);
        }
    }

    private bool TryGetForcedPreSolveAssignment(
        out KrizaljkaSlot? forcedSlot,
        out Term? forcedTerm,
        out bool isSingletonAssignment)
    {
        forcedSlot = null;
        forcedTerm = null;
        isSingletonAssignment = false;

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

            if (!TryGetFirstUnusedMatchingCollapsedTerm(slot, out var matchingTerm))
            {
                continue;
            }

            forcedSlot = slot;
            forcedTerm = matchingTerm;
            isSingletonAssignment = false;
            return true;
        }

        var singletonSlot = FindSingletonUnassignedSlot();
        if (singletonSlot is null)
        {
            return false;
        }

        if (!_currentDomainsBySlotId.TryGetValue(singletonSlot.Id, out var domain) || domain.Count != 1)
        {
            return false;
        }

        forcedSlot = singletonSlot;
        forcedTerm = domain[0];
        isSingletonAssignment = true;
        return true;
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
                        .OrderBy(t => t.DenseValue, StringComparer.OrdinalIgnoreCase)
                        .ThenBy(t => t.Id)
                        .ToList());
        }

        if (_normalizedTermsById.Count == 0)
        {
            _normalizedTermsById = normalizedTerms.ToDictionary(x => x.Id);
        }

        if (_termIdsByLengthAndLettersKey.Count == 0)
        {
            _termIdsByLengthAndLettersKey = normalizedTerms
                .GroupBy(x => x.Length)
                .ToDictionary(
                    lengthGroup => lengthGroup.Key,
                    lengthGroup =>
                        (IReadOnlyDictionary<string, IReadOnlyList<long>>)lengthGroup
                            .GroupBy(term => CreateLettersKey(term.Letters))
                            .ToDictionary(
                                group => group.Key,
                                group => (IReadOnlyList<long>)group
                                    .OrderBy(t => t.Id)
                                    .Select(t => t.Id)
                                    .ToList(),
                                StringComparer.Ordinal));
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
                                .ThenBy(t => t.Id)
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
        if (HasTimedOut())
        {
            return false;
        }

        _recursiveCalls++;
        CaptureBestStateIfNeeded();

        if (!TryGetBestNextSlot(slots, out var nextSlot))
        {
            if (!_timedOut)
            {
                _deadEnds++;
            }

            return false;
        }

        if (nextSlot is null)
        {
            return true;
        }

        foreach (var term in GetOrderedTerms(nextSlot))
        {
            _candidateTries++;

            if (!Fits(nextSlot, term))
            {
                continue;
            }

            var domainSnapshot = CloneDomains();
            var initialPlacement = Place(nextSlot, term);

            if (!TryPropagateSingletonDomainsAfterPlacement(initialPlacement, out var fullPlacement))
            {
                if (!_timedOut)
                {
                    _backtracks++;
                    _deadEnds++;
                }

                Undo(fullPlacement);
                RestoreDomains(domainSnapshot);
                continue;
            }

            if (Solve(slots))
            {
                return true;
            }

            if (!_timedOut)
            {
                _backtracks++;
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

            _singletonAutoAssignments++;

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

            var domain = CollapseToDistinctAvailableTerms(GetIndexedMatchingTerms(slot));

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
        HashSet<int> affectedSlotIds = [];

        foreach (var (assignedSlotId, _) in placement.AssignedSlots)
        {
            _currentDomainsBySlotId.Remove(assignedSlotId);

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

        foreach (var slotId in _currentDomainsBySlotId.Keys.ToList())
        {
            _currentDomainsBySlotId[slotId] = RefreshCollapsedDomainTerms(_currentDomainsBySlotId[slotId]);
        }

        foreach (var slotId in affectedSlotIds)
        {
            if (!theKrizaljka.SlotsById.TryGetValue(slotId, out var slot))
            {
                return false;
            }

            var domain = CollapseToDistinctAvailableTerms(GetIndexedMatchingTerms(slot));
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

                if (!TryGetFirstUnusedMatchingCollapsedTerm(neighborSlot, out var matchingTerm))
                {
                    continue;
                }

                _fullyFilledAutoAssignments++;

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
        CaptureBestStateIfNeeded();
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

        if (!_currentDomainsBySlotId.TryGetValue(slot.Id, out var domain))
        {
            return [];
        }

        foreach (var term in domain)
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
            .ThenBy(x => x.Term.Id)
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
            HashSet<string> distinctWithoutIndex = new(StringComparer.Ordinal);

            foreach (var term in allTerms)
            {
                if (theKrizaljka.State.UsedTermsIds.Contains(term.Id) || term.Id == sourceTerm.Id)
                {
                    continue;
                }

                distinctWithoutIndex.Add(CreateLettersKey(term.Letters));
            }

            return distinctWithoutIndex.Count;
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

        HashSet<string> distinctMatches = new(StringComparer.Ordinal);

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
                distinctMatches.Add(CreateLettersKey(term.Letters));
            }
        }

        return distinctMatches.Count;
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

    private void ResetStats()
    {
        _recursiveCalls = 0;
        _candidateTries = 0;
        _backtracks = 0;
        _deadEnds = 0;
        _fullyFilledAutoAssignments = 0;
        _singletonAutoAssignments = 0;
        _maxAssignedSlotsReached = theKrizaljka.State.AssignedTermsBySlotId.Count;
        _maxSolveElapsedMilliseconds = null;
        _timedOut = false;
        _stopToken = CancellationToken.None;
        _bestStateSnapshot = CloneState(theKrizaljka.State);

        _stopwatch.Reset();
    }

    private void CaptureBestStateIfNeeded()
    {
        var assignedCount = theKrizaljka.State.AssignedTermsBySlotId.Count;

        if (_bestStateSnapshot is null || assignedCount > _maxAssignedSlotsReached)
        {
            _maxAssignedSlotsReached = assignedCount;
            _bestStateSnapshot = CloneState(theKrizaljka.State);
        }
    }

    private KrizaljkaSolveState GetBestStateSnapshot()
    {
        return _bestStateSnapshot is not null
            ? CloneState(_bestStateSnapshot)
            : CloneState(theKrizaljka.State);
    }

    private static KrizaljkaSolveState CloneState(KrizaljkaSolveState source)
    {
        var clone = new KrizaljkaSolveState();

        foreach (var kvp in source.AssignedTermsBySlotId)
        {
            var assigned = kvp.Value;

            clone.AssignedTermsBySlotId.Add(
                kvp.Key,
                new AssignedTerm(
                    assigned.SlotId,
                    assigned.TermId,
                    assigned.Letters.ToArray()));
        }

        foreach (var termId in source.UsedTermsIds)
        {
            clone.UsedTermsIds.Add(termId);
        }

        foreach (var kvp in source.LettersByCell)
        {
            clone.LettersByCell.Add(kvp.Key, kvp.Value);
        }

        return clone;
    }

    private KrizaljkaSolveStats BuildSolveStats(bool solved)
    {
        return new KrizaljkaSolveStats
        {
            Solved = solved,
            TimedOut = _timedOut,
            ElapsedMilliseconds = _stopwatch.ElapsedMilliseconds,
            RecursiveCalls = _recursiveCalls,
            CandidateTries = _candidateTries,
            Backtracks = _backtracks,
            DeadEnds = _deadEnds,
            FullyFilledAutoAssignments = _fullyFilledAutoAssignments,
            SingletonAutoAssignments = _singletonAutoAssignments,
            MaxAssignedSlotsReached = _maxAssignedSlotsReached,
            FinalAssignedSlots = theKrizaljka.State.AssignedTermsBySlotId.Count,
        };
    }

    private List<Term> CollapseToDistinctAvailableTerms(IEnumerable<Term> terms)
    {
        List<Term> result = [];

        foreach (var group in terms
                     .GroupBy(term => CreateLettersKey(term.Letters))
                     .OrderBy(group => group.Key, StringComparer.Ordinal))
        {
            var representative = group
                .OrderBy(term => term.DenseValue, StringComparer.Ordinal)
                .ThenBy(term => term.Id)
                .First();

            if (TryGetAvailableTermForLetters(representative.Length, representative.Letters, out var availableTerm))
            {
                result.Add(availableTerm);
            }
        }

        return result;
    }

    private List<Term> RefreshCollapsedDomainTerms(IReadOnlyList<Term> collapsedTerms)
    {
        List<Term> result = [];

        foreach (var term in collapsedTerms)
        {
            if (TryGetAvailableTermForLetters(term.Length, term.Letters, out var availableTerm))
            {
                result.Add(availableTerm);
            }
        }

        return result;
    }

    private bool TryGetAvailableTermForLetters(int length, IReadOnlyList<string> letters, out Term term)
    {
        term = null!;

        if (!_termIdsByLengthAndLettersKey.TryGetValue(length, out var byLettersKey))
        {
            return false;
        }

        var lettersKey = CreateLettersKey(letters);

        if (!byLettersKey.TryGetValue(lettersKey, out var termIds))
        {
            return false;
        }

        foreach (var termId in termIds)
        {
            if (!theKrizaljka.State.UsedTermsIds.Contains(termId) &&
                _normalizedTermsById.TryGetValue(termId, out var availableTerm))
            {
                term = availableTerm;
                return true;
            }
        }

        return false;
    }

    private bool TryGetFirstUnusedMatchingCollapsedTerm(KrizaljkaSlot slot, out Term term)
    {
        term = null!;

        var collapsedMatches = CollapseToDistinctAvailableTerms(GetIndexedMatchingTerms(slot));

        if (collapsedMatches.Count == 0)
        {
            return false;
        }

        term = collapsedMatches[0];
        return true;
    }

    private static string CreateLettersKey(IReadOnlyList<string> letters)
    {
        return string.Join("|", letters);
    }

    private bool HasTimedOut()
    {
        if (_timedOut)
        {
            return true;
        }

        if (_stopToken.IsCancellationRequested)
        {
            _timedOut = true;
            return true;
        }

        if (!_maxSolveElapsedMilliseconds.HasValue)
        {
            return false;
        }

        if (_stopwatch.ElapsedMilliseconds < _maxSolveElapsedMilliseconds.Value)
        {
            return false;
        }

        _timedOut = true;
        return true;
    }
}
